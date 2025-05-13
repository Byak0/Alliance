using Alliance.Common.Extensions.Audio.NetworkMessages.FromServer;
using Alliance.Common.Extensions.Audio.Utilities;
using Alliance.Common.Utilities;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Engine.Options;
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
		private ISampleProvider mainMusicProvider;

		private string audioDirectory;
		private float defaultSoundVolume = 1f;
		private float defaultMusicVolume = 1f;
		private float musicVolumeOffset = 0f;
		private float crossfadeDuration = 3.0f;
		private bool isFading = false;

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
			audioDirectory = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName) + "ModuleSounds/";
			InitializeAudioMappings();
			if (!GameNetwork.IsServer)
			{
				SetSoundVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume));
				SetMusicVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MusicVolume));
				NativeOptions.OnNativeOptionChanged += OnNativeOptionChanged;

				waveOutDevice = new WaveOutEvent();
				mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
				mixer.ReadFully = true;
				waveOutDevice.Init(mixer);
				waveOutDevice.Play();
			}
		}

		private void OnNativeOptionChanged(NativeOptions.NativeOptionsType changedNativeOptionsType)
		{
			if (changedNativeOptionsType == NativeOptions.NativeOptionsType.MasterVolume)
			{
				SetSoundVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume));
				SetMusicVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MusicVolume));
				UpdateMainMusic();
			}
			else if (changedNativeOptionsType == NativeOptions.NativeOptionsType.SoundVolume)
			{
				SetSoundVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume));
			}
			else if (changedNativeOptionsType == NativeOptions.NativeOptionsType.MusicVolume)
			{
				SetMusicVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MusicVolume));
				UpdateMainMusic();
			}
		}

		public void SetSoundVolume(float newVolume)
		{
			Log($"Setting sound volume to {newVolume} ({NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume)} * {NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume)})", LogLevel.Debug);
			defaultSoundVolume = newVolume;
		}

		public void SetMusicVolume(float newVolume)
		{
			Log($"Setting music volume to {newVolume}", LogLevel.Debug);
			defaultMusicVolume = newVolume;
		}

		public int GetAudioId(string fileName)
		{
			if (fileNameToAudioId.TryGetValue(fileName.ToLowerInvariant(), out var audioId))
			{
				return audioId;
			}
			return -1;
		}

		private void InitializeAudioMappings()
		{
			var files = Directory.EnumerateFiles(audioDirectory, "*.*", SearchOption.AllDirectories)
						.Where(file => file.EndsWith(".wav") || file.EndsWith(".ogg") || file.EndsWith(".mp3"))
						.OrderBy(file => file)
						.ToList();

			for (int i = 0; i < files.Count; i++)
			{
				string fileName = PathHelper.GetRelativePath(audioDirectory, (files[i])).ToLowerInvariant();
				audioIdToFileName[i] = fileName;
				fileNameToAudioId[fileName] = i;
			}

			Log($"Registered {files.Count} audio files.", LogLevel.Debug);
		}

		public string[] GetAvailableSounds()
		{
			return fileNameToAudioId.Keys.ToArray();
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

		public void PlayMainMusic(int audioId, float startingPoint = 0f, float volume = 1f, bool synchronize = false)
		{
			if (audioId < 0 || !audioIdToFileName.TryGetValue(audioId, out string fileName))
			{
				Log($"ERROR : Audio ID {audioId} not found in mappings.", LogLevel.Error);
				return;
			}

			if (synchronize)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncMusic(audioId, volume, startingPoint));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

				return;
			}

			if (isFading)
			{
				Log("Music is already fading, skipping.", LogLevel.Debug);
				return;
			}

			bool mustFade = false;

			if (mainMusicProvider != null)
			{
				mustFade = true;

				// Fade out old music
				FadeOutMusic(mainMusicProvider, crossfadeDuration);
				mainMusicProvider = null;
			}

			if (!cachedSounds.ContainsKey(fileName))
			{
				CacheSound(fileName);
			}
			CachedSound mainMusicSound = new CachedSound(cachedSounds[fileName].AudioData, cachedSounds[fileName].WaveFormat, startingPoint);

			float startingVolume = mustFade ? 0f : volume * defaultMusicVolume;
			mainMusicProvider = new VolumeSampleProvider(mainMusicSound) { Volume = startingVolume };
			mixer.AddMixerInput(mainMusicProvider);

			if (mustFade)
			{
				FadeInMusic(mainMusicProvider, volume * defaultMusicVolume, crossfadeDuration);
			}
		}

		public void PlayMainMusic(string fileName, float startingPoint = 0f, float volume = 1f, bool synchronize = false)
		{
			PlayMainMusic(GetAudioId(fileName), startingPoint, volume, synchronize);
		}

		// Fade in music over the specified duration to the target volume
		private void FadeInMusic(ISampleProvider musicProvider, float targetVolume, float duration)
		{
			isFading = true;
			Task.Run(async () =>
			{
				VolumeSampleProvider volumeSampleProvider = musicProvider as VolumeSampleProvider;
				float step = targetVolume / (duration * 1000 / 50); // 50ms steps

				while (volumeSampleProvider.Volume < targetVolume)
				{
					volumeSampleProvider.Volume += step;
					await Task.Delay(50);
				}

				isFading = false;
			});
		}

		// Fade out music over the specified duration
		private void FadeOutMusic(ISampleProvider musicProvider, float duration)
		{
			Task.Run(async () =>
			{
				VolumeSampleProvider volumeProvider = musicProvider as VolumeSampleProvider;
				float step = volumeProvider.Volume / (duration * 1000 / 50);

				while (volumeProvider.Volume > 0)
				{
					volumeProvider.Volume -= step;
					await Task.Delay(50);
				}

				mixer.RemoveMixerInput(musicProvider);
			});
		}

		/// <summary>
		/// Play a temporary, localized music at a specific position.
		/// </summary>
		public void PlayLocalizedMusic(int audioId, float volume, Vec3 soundOrigin, int maxHearingDistance = 100, float startingPoint = 0f, bool muteMainMusic = true, bool synchronize = false)
		{
			if (audioId < 0 || !audioIdToFileName.TryGetValue(audioId, out string fileName))
			{
				Log($"ERROR : Audio ID {audioId} not found in mappings.", LogLevel.Error);
				return;
			}

			if (synchronize)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncMusicLocalized(audioId, volume, startingPoint, soundOrigin, maxHearingDistance, muteMainMusic));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

				return;
			}

			// test
			if (activeStreams.TryGetValue(fileName, out var readers))
			{
				Log($"Sound {fileName} is already playing", LogLevel.Debug);
				return;
			}

			Stop(fileName);

			try
			{
				if (!cachedSounds.ContainsKey(fileName))
				{
					CacheSound(fileName);
				}
				CachedSound sound = new CachedSound(cachedSounds[fileName].AudioData, cachedSounds[fileName].WaveFormat, startingPoint);

				if (!activeStreams.ContainsKey(fileName))
				{
					activeStreams[fileName] = new List<CachedSound>();
				}

				mixer.AddMixerInput(Apply3DSpatialization(ref sound, soundOrigin, volume, maxHearingDistance));

				activeStreams[fileName].Add(sound);

				if (muteMainMusic)
				{
					// Add a watcher to mute/unmute main music
					Task.Run(async () =>
					{
						while (!sound.IsComplete)
						{
							await Task.Delay(50);
							musicVolumeOffset = sound.VolumeProvider.Volume * 1.5f;
						}
						musicVolumeOffset = 0f;
					});
				}
			}
			catch (Exception ex)
			{
				Log($"An error occurred when playing music: {ex.Message}", LogLevel.Debug);
			}
		}

		public void PlayLocalizedMusic(string fileName, float volume, Vec3 soundOrigin, int maxHearingDistance = 100, float startingPoint = 0f, bool muteMainMusic = true, bool synchronize = false)
		{
			PlayLocalizedMusic(GetAudioId(fileName), volume, soundOrigin, maxHearingDistance, startingPoint, muteMainMusic, synchronize);
		}

		public void Play(int audioId, float volume, bool stackable = true, int maxHearingDistance = 100, Vec3? soundOrigin = null, bool synchronize = false)
		{
			if (audioId < 0 || !audioIdToFileName.TryGetValue(audioId, out string fileName))
			{
				Log($"ERROR : Audio ID {audioId} not found in mappings.", LogLevel.Error);
				return;
			}

			if (synchronize)
			{
				if (soundOrigin.HasValue)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SyncAudioLocalized(audioId, volume, soundOrigin.Value, maxHearingDistance, stackable));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
				else
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SyncAudio(audioId, volume, stackable));
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
					var volumeProvider = new VolumeSampleProvider(sound) { Volume = defaultSoundVolume * volume };
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

		public void Play(string fileName, float volume, bool stackable = true, int maxHearingDistance = 100, Vec3? soundOrigin = null, bool synchronize = false)
		{
			Play(GetAudioId(fileName), volume, stackable, maxHearingDistance, soundOrigin, synchronize);
		}

		private ISampleProvider Apply3DSpatialization(ref CachedSound reader, Vec3 soundOrigin, float initialVolume, int maxHearingDistance)
		{
			Vec3 myPosition = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Position : Mission.Current.GetCameraFrame().origin;
			Mat3 myRotation = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Frame.rotation : Mission.Current.GetCameraFrame().rotation;

			float volume = defaultSoundVolume * initialVolume * AudioHelper.CalculateVolume(myPosition, soundOrigin, maxHearingDistance);
			float pan = AudioHelper.CalculatePan(soundOrigin, myPosition, myRotation);

			ISampleProvider sampleProvider = EnsureMono(reader);
			var panningProvider = new PanningSampleProvider(sampleProvider) { Pan = pan };
			var volumeProvider = new VolumeSampleProvider(panningProvider) { Volume = volume };

			ISampleProvider convertedInput = ConvertToCommonFormat(volumeProvider);

			reader.SetSpatialProviders(soundOrigin, initialVolume, maxHearingDistance, panningProvider, volumeProvider);

			return convertedInput;
		}

		public float GetSoundTickFromTimer(float timerInSeconds, CachedSound cachedSound)
		{
			float soundLength = cachedSound.AudioData.Length / ((float)cachedSound.WaveFormat.SampleRate * cachedSound.WaveFormat.Channels);
			return timerInSeconds % soundLength;
		}

		public void TickAudio()
		{
			UpdateSoundPositions();
			UpdateMainMusic();
		}

		private void UpdateMainMusic()
		{
			if (mainMusicProvider != null && !isFading)
			{
				(mainMusicProvider as VolumeSampleProvider).Volume = Math.Max(0f, defaultMusicVolume - musicVolumeOffset);
				Log($"New volume for main music is {(mainMusicProvider as VolumeSampleProvider).Volume}", LogLevel.Debug);
			}
		}

		private void UpdateSoundPositions()
		{
			Vec3 currentPosition = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Position : Mission.Current.GetCameraFrame().origin;
			Mat3 currentRotation = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Frame.rotation : Mission.Current.GetCameraFrame().rotation;
			foreach (var activeSounds in activeStreams.Values)
			{
				foreach (CachedSound sound in activeSounds)
				{
					if (sound.PanningProvider == null && sound.VolumeProvider != null)
					{
						sound.VolumeProvider.Volume = defaultSoundVolume * sound.InitialVolume;
						Log($"New volume for 2d sound is {sound.VolumeProvider.Volume}", LogLevel.Debug);
					}
					else if (sound.SoundOrigin != null && sound.PanningProvider != null && sound.VolumeProvider != null)
					{
						Vec3 soundOrigin = sound.SoundOrigin.Value;
						sound.PanningProvider.Pan = AudioHelper.CalculatePan(soundOrigin, currentPosition, currentRotation);
						sound.VolumeProvider.Volume = defaultSoundVolume * sound.InitialVolume * AudioHelper.CalculateVolume(currentPosition, soundOrigin, sound.MaxHearingDistance);
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
				foreach (var reader in readers)
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
		public bool IsComplete => ReadProgress >= AudioData.Length;
		public PanningSampleProvider PanningProvider { get; private set; }
		public VolumeSampleProvider VolumeProvider { get; private set; }
		public Vec3? SoundOrigin { get; private set; }
		public float InitialVolume { get; private set; }
		public int MaxHearingDistance { get; private set; }
		public int ReadProgress { get; private set; }

		// Constructor that accepts a start time in seconds
		public CachedSound(float[] audioData, WaveFormat waveFormat, float startTimeInSeconds = 0f)
		{
			AudioData = audioData;
			WaveFormat = waveFormat;

			// Calculate the sound length in seconds
			float soundLength = AudioData.Length / ((float)WaveFormat.SampleRate * WaveFormat.Channels);
			// Calculate the starting point in seconds (modulus to ensure it's within bounds)
			float startingPoint = startTimeInSeconds % soundLength;
			// Calculate the starting point in samples
			ReadProgress = (int)(startingPoint * WaveFormat.SampleRate * WaveFormat.Channels);

			Log($"Read sound from: {startingPoint}/{soundLength}, starting sample: {ReadProgress}/{AudioData.Length}", LogLevel.Debug);
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
			// No more samples to read
			if (ReadProgress >= AudioData.Length)
			{
				return 0;
			}

			// Number of samples remaining in the audio data
			int availableSamples = AudioData.Length - ReadProgress;
			int samplesToCopy = Math.Min(availableSamples, count); // Copy only as many samples as are available
			Array.Copy(AudioData, ReadProgress, buffer, offset, samplesToCopy); // Copy samples from current progress into the output buffer

			// Update the read progress by the number of samples copied
			ReadProgress += samplesToCopy;

			// Return the number of samples that were actually copied
			return samplesToCopy;
		}
	}
}