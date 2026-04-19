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
		private Dictionary<int, string> audioIdToFile = new Dictionary<int, string>();
		private Dictionary<int, string> audioIdToFileName = new Dictionary<int, string>();
		private Dictionary<string, int> fileNameToAudioId = new Dictionary<string, int>();
		private Dictionary<int, CachedSound> cachedSounds = new Dictionary<int, CachedSound>();
		private Dictionary<int, List<CachedSound>> activeStreams = new Dictionary<int, List<CachedSound>>();
		private ISampleProvider mainMusicProvider;

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
			InitializeAudioMappings();
			if (!GameNetwork.IsServer)
			{
				SetSoundVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume));
				SetSoundVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MusicVolume));
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
			var allFiles = new List<(string moduleName, string filePath)>();

			// Scan all loaded modules for ModuleSounds directories
			foreach (var moduleInfo in ModuleHelper.GetModules())
			{
				string moduleAudioDir = ModuleHelper.GetModuleFullPath(moduleInfo.Id) + "ModuleSounds/";
				if (!Directory.Exists(moduleAudioDir)) continue;

				var moduleFiles = Directory.EnumerateFiles(moduleAudioDir, "*.*", SearchOption.AllDirectories)
							.Where(file => file.EndsWith(".wav") || file.EndsWith(".ogg") || file.EndsWith(".mp3"))
							.Select(file => (moduleInfo.Id, file))
							.ToList();

				allFiles.AddRange(moduleFiles);
			}

			// Sort by module name then file path for deterministic ordering
			allFiles = allFiles.OrderBy(x => x.moduleName).ThenBy(x => x.filePath).ToList();

			for (int i = 0; i < allFiles.Count; i++)
			{
				string moduleName = allFiles[i].moduleName;
				string moduleAudioDir = ModuleHelper.GetModuleFullPath(moduleName) + "ModuleSounds/";
				string relativeFileName = PathHelper.GetRelativePath(moduleAudioDir, allFiles[i].filePath);

				// Full key with module prefix: "Alliance/Native/Alert/horn.mp3"
				string prefixedKey = $"{moduleName}/{relativeFileName}".ToLowerInvariant();

				audioIdToFile[i] = allFiles[i].filePath;
				audioIdToFileName[i] = prefixedKey;
				fileNameToAudioId[prefixedKey] = i;
			}

			Log($"Registered {allFiles.Count} audio files from {allFiles.Select(x => x.moduleName).Distinct().Count()} modules.", LogLevel.Debug);
		}

		public string[] GetAvailableSounds()
		{
			return fileNameToAudioId.Keys.ToArray();
		}

		private void CacheSound(int audioId)
		{
			var filePath = audioIdToFile[audioId];
			if (!cachedSounds.ContainsKey(audioId))
			{
				var reader = new AudioFileReader(filePath);
				var buffer = new List<float>((int)(reader.Length / 4));
				var readBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
				int samplesRead;
				while ((samplesRead = reader.Read(readBuffer, 0, readBuffer.Length)) > 0)
				{
					buffer.AddRange(readBuffer.Take(samplesRead));
				}
				cachedSounds[audioId] = new CachedSound(buffer.ToArray(), reader.WaveFormat);
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

			if (!cachedSounds.ContainsKey(audioId))
			{
				CacheSound(audioId);
			}
			CachedSound mainMusicSound = new CachedSound(cachedSounds[audioId].AudioData, cachedSounds[audioId].WaveFormat, startingPoint);

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
			if (activeStreams.TryGetValue(audioId, out var readers))
			{
				Log($"Sound {fileName} is already playing", LogLevel.Debug);
				return;
			}

			Stop(audioId);

			try
			{
				if (!cachedSounds.ContainsKey(audioId))
				{
					CacheSound(audioId);
				}
				CachedSound sound = new CachedSound(cachedSounds[audioId].AudioData, cachedSounds[audioId].WaveFormat, startingPoint);

				if (!activeStreams.ContainsKey(audioId))
				{
					activeStreams[audioId] = new List<CachedSound>();
				}

				mixer.AddMixerInput(Apply3DSpatialization(ref sound, soundOrigin, volume, maxHearingDistance));

				activeStreams[audioId].Add(sound);

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
				Stop(audioId);
			}

			try
			{
				if (!cachedSounds.ContainsKey(audioId))
				{
					CacheSound(audioId);
				}
				CachedSound sound = new CachedSound(cachedSounds[audioId].AudioData, cachedSounds[audioId].WaveFormat);

				if (!activeStreams.ContainsKey(audioId))
				{
					activeStreams[audioId] = new List<CachedSound>();
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
				activeStreams[audioId].Add(sound);
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
						//Log($"New pan/volume for 3d sound {soundOrigin} is {sound.PanningProvider.Pan}/{sound.VolumeProvider.Volume}", LogLevel.Debug);
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

		/// <summary>
		/// Play a looping 3D sound. Returns the CachedSound handle for position updates and stopping.
		/// </summary>
		public CachedSound PlayLooping(int audioId, float volume, int maxHearingDistance = 100, Vec3? soundOrigin = null)
		{
			if (mixer == null) return null;

			if (audioId < 0 || !audioIdToFileName.TryGetValue(audioId, out string fileName))
			{
				Log($"ERROR : Audio ID {audioId} not found in mappings.", LogLevel.Error);
				return null;
			}

			try
			{
				if (!cachedSounds.ContainsKey(audioId))
				{
					CacheSound(audioId);
				}
				CachedSound sound = new CachedSound(cachedSounds[audioId].AudioData, cachedSounds[audioId].WaveFormat);
				sound.Loop = true;

				if (!activeStreams.ContainsKey(audioId))
				{
					activeStreams[audioId] = new List<CachedSound>();
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
				activeStreams[audioId].Add(sound);
				return sound;
			}
			catch (Exception ex)
			{
				Log($"An error occurred when playing looping audio: {ex.Message}", LogLevel.Debug);
				return null;
			}
		}

		public CachedSound PlayLooping(string fileName, float volume, int maxHearingDistance = 100, Vec3? soundOrigin = null)
		{
			return PlayLooping(GetAudioId(fileName), volume, maxHearingDistance, soundOrigin);
		}

		public void Stop(int audioId)
		{
			if (activeStreams.TryGetValue(audioId, out var readers))
			{
				foreach (var reader in readers)
				{
					mixer.RemoveMixerInput(reader);
				}
				activeStreams.Remove(audioId);
			}
		}

		public void StopAll()
		{
			foreach (var audioId in new List<int>(activeStreams.Keys))
			{
				Stop(audioId);
			}
		}

		public void Dispose()
		{
			StopAll();
			waveOutDevice.Dispose();
		}

		public void CleanSounds()
		{
			foreach (var audioId in new List<int>(activeStreams.Keys))
			{
				if (activeStreams[audioId].All(sound => sound.IsComplete))
				{
					Stop(audioId);
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
		public bool Loop { get; set; }
		/// <summary>
		/// Playback speed multiplier. Also affects pitch (higher = faster + higher pitch).
		/// </summary>
		public float PlaybackRate { get; set; } = 1.0f;
		/// <summary>
		/// Duration in seconds of the crossfade applied at the loop boundary to eliminate clicks.
		/// </summary>
		public float CrossfadeSeconds { get; set; }
		public bool IsComplete => _stopped || (!Loop && _readPosition >= _totalFrames);
		public PanningSampleProvider PanningProvider { get; private set; }
		public VolumeSampleProvider VolumeProvider { get; private set; }
		public Vec3? SoundOrigin { get; private set; }
		public float InitialVolume { get; private set; }
		public int MaxHearingDistance { get; private set; }
		public int ReadProgress => (int)(_readPosition * _channels);
		private bool _stopped;
		private float _readPosition; // current position in frames
		private int _channels;
		private int _totalFrames;

		public CachedSound(float[] audioData, WaveFormat waveFormat, float startTimeInSeconds = 0f)
		{
			AudioData = audioData;
			WaveFormat = waveFormat;
			_channels = waveFormat.Channels;
			_totalFrames = audioData.Length / _channels;

			float soundLength = _totalFrames / (float)WaveFormat.SampleRate;
			float startingPoint = startTimeInSeconds % soundLength;
			_readPosition = startingPoint * WaveFormat.SampleRate;
		}

		public void SetSpatialProviders(Vec3? soundOrigin, float initialVolume, int maxHearingRange, PanningSampleProvider panningProvider, VolumeSampleProvider volumeProvider)
		{
			SoundOrigin = soundOrigin;
			InitialVolume = initialVolume;
			MaxHearingDistance = maxHearingRange;
			PanningProvider = panningProvider;
			VolumeProvider = volumeProvider;
		}

		/// <summary>
		/// Stop this sound instance. It will return silence and be cleaned up.
		/// </summary>
		public void Stop()
		{
			_stopped = true;
			Loop = false;
		}

		/// <summary>
		/// Update the 3D origin of this sound for spatialization.
		/// </summary>
		public void SetSoundOrigin(Vec3 origin)
		{
			SoundOrigin = origin;
		}

		/// <summary>
		/// Update the initial volume used for 3D spatialization calculations.
		/// </summary>
		public void SetVolume(float volume)
		{
			InitialVolume = volume;
		}

		public int Read(float[] buffer, int offset, int count)
		{
			if (_stopped) return 0;

			int framesRequested = count / _channels;
			int framesWritten = 0;
			int crossfadeFrames = (int)(CrossfadeSeconds * WaveFormat.SampleRate);
			// Guard against files too short to crossfade cleanly
			if (crossfadeFrames * 2 >= _totalFrames) crossfadeFrames = 0;

			while (framesWritten < framesRequested)
			{
				if (_readPosition >= _totalFrames)
				{
					if (!Loop) break;
					// Jump to crossfadeFrames (not 0) so the zone already blended at the
					// end is not replayed — that double-play was causing the loop artifact.
					_readPosition -= _totalFrames - crossfadeFrames;
				}

				int frame0 = (int)_readPosition;
				float frac = _readPosition - frame0;
				int frame1 = Math.Min(frame0 + 1, _totalFrames - 1);

				int bufPos = offset + framesWritten * _channels;

				// Compute crossfade weights once per frame, outside the channel loop
				float distFromEnd = _totalFrames - _readPosition;
				bool inCrossfade = Loop && crossfadeFrames > 0 && distFromEnd < crossfadeFrames;
				float crossfadeT = inCrossfade ? distFromEnd / crossfadeFrames : 1f;
				float startPos = inCrossfade ? crossfadeFrames - distFromEnd : 0f;
				int sf0 = inCrossfade ? (int)startPos : 0;
				int sf1 = inCrossfade ? Math.Min(sf0 + 1, _totalFrames - 1) : 0;
				float sfrac = inCrossfade ? startPos - sf0 : 0f;

				for (int ch = 0; ch < _channels; ch++)
				{
					float s0 = AudioData[frame0 * _channels + ch];
					float s1 = AudioData[frame1 * _channels + ch];
					float sample = s0 + (s1 - s0) * frac;

					if (inCrossfade)
					{
						float ss0 = AudioData[sf0 * _channels + ch];
						float ss1 = AudioData[sf1 * _channels + ch];
						sample = sample * crossfadeT + (ss0 + (ss1 - ss0) * sfrac) * (1f - crossfadeT);
					}

					buffer[bufPos + ch] = sample;
				}

				_readPosition += PlaybackRate;
				framesWritten++;
			}

			return framesWritten * _channels;
		}
	}
}