using Alliance.Common.Extensions.Audio.NetworkMessages.FromServer;
using Alliance.Common.Extensions.Audio.Utilities;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.Audio
{
    /// <summary>
    /// Custom audio player based on NAudio. 
    /// Cannot be used for native sounds (use NativeAudioPlayer instead).
    /// </summary>
    public class AudioPlayer
    {
        private IWavePlayer waveOutDevice;
        private MixingSampleProvider mixer;
        private Dictionary<int, string> audioIdToFileName = new Dictionary<int, string>();
        private Dictionary<string, int> fileNameToAudioId = new Dictionary<string, int>();
        private Dictionary<string, CachedSound> cachedSounds = new Dictionary<string, CachedSound>();
        private Dictionary<string, List<CachedSound>> activeStreams = new Dictionary<string, List<CachedSound>>();
        private string audioDirectory;
        private float defaultVolume = 1f;

        private static AudioPlayer _instance;

        public static AudioPlayer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AudioPlayer();
                }
                return _instance;
            }
        }

        public AudioPlayer()
        {
            audioDirectory = ModuleHelper.GetModuleFullPath("Alliance") + "ModuleSounds/";
            InitializeAudioMappings();          
            if(GameNetwork.IsClient)
            {
                waveOutDevice = new WaveOutEvent();
                mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                mixer.ReadFully = true;
                waveOutDevice.Init(mixer);
                waveOutDevice.Play();
            }                
        }

        public void SetVolume(float newVolume)
        {
            Log($"Setting volume to {newVolume}", LogLevel.Debug);
            defaultVolume = newVolume;
        }

        public int GetAudioId(string fileName)
        {
            if (fileNameToAudioId.TryGetValue(fileName, out var audioId))
            {
                return audioId;
            }
            return -1;
        }

        private void InitializeAudioMappings()
        {
            var files = Directory.EnumerateFiles(audioDirectory)
                        .Where(file => file.EndsWith(".wav") || file.EndsWith(".ogg") || file.EndsWith(".mp3"))
                        .OrderBy(file => file)
                        .ToList();

            for (int i = 0; i < files.Count; i++)
            {
                string fileName = Path.GetFileName(files[i]);
                audioIdToFileName[i] = fileName;
                fileNameToAudioId[fileName] = i;
            }

            Log($"Registered {files.Count} audio files.", LogLevel.Debug);
        }        

        private void CacheSound(string fileName)
        {
            var filePath = Path.Combine(audioDirectory, fileName);
            if (!cachedSounds.ContainsKey(fileName))
            {
                var reader = new AudioFileReader(filePath);
                var buffer = new List<float>((int)(reader.Length / 4));
                var readBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = reader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    buffer.AddRange(readBuffer.Take(samplesRead));
                }
                cachedSounds[fileName] = new CachedSound(buffer.ToArray(), reader.WaveFormat);
                reader.Dispose();
            }
        }

        public void Play(int audioId, float volume, bool stackable = true, int maxHearingDistance = 100, Vec3? soundOrigin = null, bool synchronize = false)
        {
            if (audioIdToFileName.TryGetValue(audioId, out string fileName))
            {
                Play(fileName, volume, stackable, maxHearingDistance, soundOrigin, synchronize);
            } 
            else
            {
                Log($"ERROR : Audio ID {audioId} not found in mappings.", LogLevel.Error);
            }
        }

        public void Play(string fileName, float volume, bool stackable = true, int maxHearingDistance = 100, Vec3? soundOrigin = null, bool synchronize = false)
        {
            if (synchronize)
            {
                if (soundOrigin.HasValue)
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new SyncAudioLocalized(fileNameToAudioId[fileName], volume, soundOrigin.Value, maxHearingDistance, stackable));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                } 
                else
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new SyncAudio(fileNameToAudioId[fileName], volume, stackable));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                }
                    
                return;
            }

            if (!stackable)
            {
                Stop(fileName);
            }

            try
            {
                if (!cachedSounds.ContainsKey(fileName))
                {
                    CacheSound(fileName);
                }
                CachedSound sound = new CachedSound(cachedSounds[fileName].AudioData, cachedSounds[fileName].WaveFormat);

                if (!activeStreams.ContainsKey(fileName))
                {
                    activeStreams[fileName] = new List<CachedSound>();
                }
                
                if (soundOrigin.HasValue)
                {
                    mixer.AddMixerInput(Apply3DSpatialization(ref sound, soundOrigin.Value, volume, maxHearingDistance));
                }
                else
                {
                    var volumeProvider = new VolumeSampleProvider(sound) { Volume = defaultVolume * volume };
                    ISampleProvider convertedInput = ConvertToCommonFormat(volumeProvider);
                    mixer.AddMixerInput(convertedInput);
                }
                activeStreams[fileName].Add(sound);
            }
            catch (Exception ex)
            {
                Log($"An error occurred when playing audio: {ex.Message}", LogLevel.Debug);
            }
        }

        private ISampleProvider Apply3DSpatialization(ref CachedSound reader, Vec3 soundOrigin, float initialVolume, int maxHearingDistance)
        {
            Vec3 myPosition = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Position : Mission.Current.GetCameraFrame().origin;
            Mat3 myRotation = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Frame.rotation : Mission.Current.GetCameraFrame().rotation;

            float volume = defaultVolume * initialVolume * AudioHelper.CalculateVolume(myPosition, soundOrigin, maxHearingDistance);
            float pan = AudioHelper.CalculatePan(soundOrigin, myPosition, myRotation);

            ISampleProvider sampleProvider = EnsureMono(reader);
            var panningProvider = new PanningSampleProvider(sampleProvider) { Pan = pan };
            var volumeProvider = new VolumeSampleProvider(panningProvider) { Volume = volume };

            ISampleProvider convertedInput = ConvertToCommonFormat(volumeProvider);

            reader.SetSpatialProviders(soundOrigin, initialVolume, maxHearingDistance, panningProvider, volumeProvider);

            return convertedInput;
        }

        public void UpdateSoundPositions()
        {
            Vec3 currentPosition = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Position : Mission.Current.GetCameraFrame().origin;
            Mat3 currentRotation = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Frame.rotation : Mission.Current.GetCameraFrame().rotation;
            foreach (var activeSounds in activeStreams.Values)
            {
                foreach(CachedSound sound in activeSounds)
                {
                    if(sound.PanningProvider == null && sound.VolumeProvider != null)
                    {
                        sound.VolumeProvider.Volume = defaultVolume * sound.InitialVolume;
                        Log($"New volume for 2d sound is {sound.VolumeProvider.Volume}", LogLevel.Debug);
                    }
                    else if (sound.SoundOrigin != null && sound.PanningProvider != null && sound.VolumeProvider != null)
                    {
                        Vec3 soundOrigin = sound.SoundOrigin.Value;
                        sound.PanningProvider.Pan = AudioHelper.CalculatePan(soundOrigin, currentPosition, currentRotation);
                        sound.VolumeProvider.Volume = defaultVolume * sound.InitialVolume * AudioHelper.CalculateVolume(currentPosition, soundOrigin, sound.MaxHearingDistance);
                        Log($"New pan/volume for 3d sound {soundOrigin} is {sound.PanningProvider.Pan}/{sound.VolumeProvider.Volume}", LogLevel.Debug);
                    }
                    
                }
            }
        }

        private ISampleProvider EnsureMono(ISampleProvider source)
        {
            if (source.WaveFormat.Channels == 1)
            {
                return source; // Already mono, no conversion needed
            }
            else if (source.WaveFormat.Channels == 2)
            {
                // Convert stereo to mono
                return new StereoToMonoSampleProvider(source);
            }
            else
            {
                throw new NotImplementedException("Mono conversion for sources with more than two channels is not implemented.");
            }
        }

        public void Stop(string fileName)
        {
            if (activeStreams.TryGetValue(fileName, out var readers))
            {
                foreach(var reader in readers)
                {
                    mixer.RemoveMixerInput(reader);
                }
                activeStreams.Remove(fileName);
            }
        }

        public void StopAll()
        {
            foreach (var fileName in new List<string>(activeStreams.Keys))
            {
                Stop(fileName);
            }
        }

        public void Dispose()
        {
            StopAll();
            waveOutDevice.Dispose();
        }

        public void CleanSounds()
        {
            foreach (var fileName in new List<string>(activeStreams.Keys))
            {
                if (activeStreams[fileName].All(sound => sound.IsComplete))
                {
                    Stop(fileName);
                }
            }
        }

        private ISampleProvider ConvertToCommonFormat(ISampleProvider input)
        {
            if (input.WaveFormat.Equals(mixer.WaveFormat))
            {
                // No conversion needed
                return input; 
            }

            // Resample or convert channel count as necessary
            ISampleProvider resampled = new WdlResamplingSampleProvider(input, mixer.WaveFormat.SampleRate);
            if (mixer.WaveFormat.Channels == 2 && input.WaveFormat.Channels == 1)
            {
                // Convert mono to stereo
                return new MonoToStereoSampleProvider(resampled);
            }
            else if (mixer.WaveFormat.Channels == 1 && input.WaveFormat.Channels == 2)
            {
                // Optionally, convert stereo to mono
                return new StereoToMonoSampleProvider(resampled);
            }

            return resampled;
        }
    }

    public class CachedSound : ISampleProvider
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }
        public bool IsComplete => readProgress >= AudioData.Length;
        public PanningSampleProvider PanningProvider { get; private set; }
        public VolumeSampleProvider VolumeProvider { get; private set; }
        public Vec3? SoundOrigin { get; private set; }
        public float InitialVolume { get; private set; }
        public int MaxHearingDistance { get; private set; }
        private int readProgress;

        public CachedSound(float[] audioData, WaveFormat waveFormat)
        {
            AudioData = audioData;
            WaveFormat = waveFormat;
            readProgress = 0;
        }

        public void SetSpatialProviders(Vec3? soundOrigin, float initialVolume, int maxHearingRange, PanningSampleProvider panningProvider, VolumeSampleProvider volumeProvider)
        {
            SoundOrigin = soundOrigin;
            InitialVolume = initialVolume;
            MaxHearingDistance = maxHearingRange;
            PanningProvider = panningProvider;
            VolumeProvider = volumeProvider;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int availableSamples = AudioData.Length - readProgress;
            int samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(AudioData, readProgress, buffer, offset, samplesToCopy);
            readProgress += samplesToCopy;
            return samplesToCopy;
        }
    }
}