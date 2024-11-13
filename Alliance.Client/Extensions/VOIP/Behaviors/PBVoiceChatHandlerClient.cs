using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.Audio.Utilities;
using Alliance.Common.Extensions.VOIP.NetworkMessages.FromClient;
using Alliance.Common.Extensions.VOIP.NetworkMessages.FromServer;
using Concentus.Enums;
using Concentus.Structs;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.VOIP.Behaviors
{
	/// <summary>
	/// VOIP system made by the Persistent Bannerlord team.
	/// Credits to PersistentBannerlord@2023.
	/// </summary>
	public class PBVoiceChatHandlerClient : MissionNetwork
	{
		const int VOICE_RECORD_MAX_CHUNK_SIZE_IN_BYTES = 72000;
		const int VOICE_FRAME_RAW_SIZE_IN_BYTES = 1440;
		const int COMPRESSION_MAX_CHUNK_SIZE_IN_BYTES = 8640;
		const int OPUS_FRAME_SIZE = 720;
		const int SAMPLE_RATE = 12000;

		public event Action<MissionPeer, bool, bool> OnPeerVoiceStatusUpdated;
		public event Action<Agent, bool, bool> OnBotVoiceStatusUpdated;
		public event Action OnVoiceRecordStarted;
		public event Action OnVoiceRecordStopped;

		// Voice buffers of incoming players
		private List<PlayerVoiceData> _playerVoiceList = new List<PlayerVoiceData>();
		private WaveInEvent _waveIn;
		private WaveOutEvent _waveOut;
		private BufferedWaveProvider _waveProvider;
		private MixingSampleProvider _audioMixer;
		private MuffleFilter _muffleFilter;
		private OpusEncoder _encoder;
		private Thread _audioThread;
		private volatile bool _running = true;
		private bool _isAnnouncement;
		private bool _playedAnyVoicePreviousTick;
		private bool _isVoiceRecordActive;
		private Queue<byte> _voiceToSend;
		private bool _stopRecordingOnNextTick;
		private bool _isVoiceChatDisabled = true;

		// Buffer to hold OnWaveDataAvailable event data for later OnTick voice record
		private Queue<byte> _voiceBufferQueue = new Queue<byte>();
		private object _voiceBufferLock = new object();
		private float _voipInVolume = 1f;
		private float _voipOutVolume = 1f;

		private void EnqueueVoiceData(byte[] data, int length)
		{
			lock (_voiceBufferLock)
			{
				for (int i = 0; i < length; i++)
				{
					_voiceBufferQueue.Enqueue(data[i]);
				}
			}
		}

		private void DequeueVoiceData(byte[] buffer, int bufferSize, out int readBytesLength)
		{
			lock (_voiceBufferLock)
			{
				readBytesLength = Math.Min(bufferSize, _voiceBufferQueue.Count);

				for (int i = 0; i < readBytesLength; i++)
				{
					buffer[i] = _voiceBufferQueue.Dequeue();
				}
			}
		}

		/// <summary>
		/// Client side - Handle players voice records sent by server.
		/// </summary>
		public void HandleServerEventSendVoiceToPlay(GameNetworkMessage baseMessage)
		{
			SendVoiceToPlay sendVoiceToPlay = (SendVoiceToPlay)baseMessage;

			if (_isVoiceChatDisabled)
			{
				return;
			}

			MissionPeer component = sendVoiceToPlay.Peer.GetComponent<MissionPeer>();
			if (component == null || sendVoiceToPlay.BufferLength <= 0 || component.IsMutedFromGameOrPlatform)
			{
				return;
			}

			for (int i = 0; i < _playerVoiceList.Count; i++)
			{
				if (_playerVoiceList[i].Peer == component)
				{
					// Decode the compressed voice data
					OpusDecoder decoder = _playerVoiceList[i].GetDecoder();
					short[] outputBuffer = new short[OPUS_FRAME_SIZE];
					decoder.Decode(sendVoiceToPlay.Buffer, 0, sendVoiceToPlay.BufferLength, outputBuffer, 0, OPUS_FRAME_SIZE, false);

					// Write the decoded voice data to the player's voice data buffer
					_playerVoiceList[i].WriteVoiceData(outputBuffer);
					break;
				}
			}
		}

		/// <summary>
		/// Client side - Handle bot voice records sent by server.
		/// </summary>
		public void HandleServerEventSendBotVoiceToPlay(GameNetworkMessage message)
		{
			SendBotVoiceToPlay sendVoiceToPlay = (SendBotVoiceToPlay)message;
			if (_isVoiceChatDisabled)
			{
				return;
			}

			if (sendVoiceToPlay.BufferLength <= 0)
			{
				return;
			}

			for (int i = 0; i < _playerVoiceList.Count; i++)
			{
				if (_playerVoiceList[i].Agent == sendVoiceToPlay.Agent)
				{
					// Decode the compressed voice data
					OpusDecoder decoder = _playerVoiceList[i].GetDecoder();
					short[] outputBuffer = new short[OPUS_FRAME_SIZE];
					decoder.Decode(sendVoiceToPlay.Buffer, 0, sendVoiceToPlay.BufferLength, outputBuffer, 0, OPUS_FRAME_SIZE, false);

					// Write the decoded voice data to the player's voice data buffer
					_playerVoiceList[i].WriteVoiceData(outputBuffer);
					break;
				}
			}
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			if (GameNetwork.IsClient)
			{
				_playerVoiceList = new List<PlayerVoiceData>();
				_voiceToSend = new Queue<byte>();

				_voipInVolume = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.VoiceOverVolume);
				_voipOutVolume = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.VoiceChatVolume);

				_waveIn = new WaveInEvent();
				_waveIn.DeviceNumber = 0;
				_waveIn.BufferMilliseconds = 100;
				_waveIn.WaveFormat = new WaveFormat(SAMPLE_RATE, 16, 1);
				_waveIn.DataAvailable += OnWaveInDataAvailable;

				// Initialize WaveOutEvent for playback
				_waveOut = new WaveOutEvent();
				_waveOut.DeviceNumber = 0;

				WaveFormat waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SAMPLE_RATE, 1);

				_waveProvider = new BufferedWaveProvider(new WaveFormat(SAMPLE_RATE, 16, 1));
				_waveProvider.BufferLength = VOICE_RECORD_MAX_CHUNK_SIZE_IN_BYTES * 3;
				_waveOut.Init(_waveProvider);

				_muffleFilter = new MuffleFilter(SAMPLE_RATE);

				_audioMixer = new MixingSampleProvider(waveFormat);
				_waveOut.Init(_audioMixer);
				_audioMixer.ReadFully = true;

				// Compress audio data using Concentus
				_encoder = OpusEncoder.Create(SAMPLE_RATE, 1, OpusApplication.OPUS_APPLICATION_AUDIO);
				_encoder.Bitrate = 16000;

				HandleOptionsUpdated();
			}
		}

		public void StartAudioProcessing()
		{
			_audioThread = new Thread(AudioLoop) { IsBackground = true };
			_audioThread.Start();
		}

		private void AudioLoop()
		{
			_waveOut.Play(); // Start playback once

			while (_running)
			{
				const int sampleBufferSize = 2500; // Example buffer size
				float[] mixedSamples = new float[sampleBufferSize];

				int bytesRead = _audioMixer.Read(mixedSamples, 0, mixedSamples.Length);
				if (bytesRead > 0)
				{
					byte[] byteBuffer = new byte[bytesRead * 2]; // Assuming float to 16-bit conversion
					Buffer.BlockCopy(mixedSamples, 0, byteBuffer, 0, bytesRead * 2);
					// Check to prevent buffer overflow
					if (_waveProvider.BufferedBytes + byteBuffer.Length > _waveProvider.BufferLength)
					{
						_waveProvider.ClearBuffer();
					}

					_waveProvider.AddSamples(byteBuffer, 0, byteBuffer.Length);
				}
				else
				{
					// Optionally sleep a minimal amount to prevent a tight loop that hogs CPU
					Thread.Sleep(5);
				}
			}
		}

		public void StopAudioProcessing()
		{
			_running = false; // Signal the loop to terminate
			if (_audioThread != null && _audioThread.IsAlive)
			{
				_audioThread.Join(); // Wait for the thread to finish executing
			}
		}

		private async void OnWaveInDataAvailable(object sender, WaveInEventArgs e)
		{
			if ((Mission.MainAgent == null || !Mission.MainAgent.IsActive()) && !_isAnnouncement)
			{
				return;
			}

			WaveInEvent waveIn = (WaveInEvent)sender;

			// Create short array to hold the samples
			short[] samples = new short[e.BytesRecorded / 2];

			// Convert raw bytes to 16-bit samples
			Buffer.BlockCopy(e.Buffer, 0, samples, 0, e.BytesRecorded);


			ProcessVoiceFiltering(samples, out byte[] buffer, out int bufferLength);

			EnqueueVoiceData(buffer, bufferLength);
		}

		public override void AfterStart()
		{
			UpdateVoiceChatEnabled();

			NativeOptions.OnNativeOptionChanged += OnNativeOptionChanged;
			ManagedOptions.OnManagedOptionChanged += OnManagedOptionChanged;
		}

		public override void OnRemoveBehavior()
		{
			_waveProvider?.ClearBuffer();
			_waveOut?.Dispose();
			_waveIn?.Dispose();
			_waveIn.DataAvailable -= OnWaveInDataAvailable;
			StopAudioProcessing();

			NativeOptions.OnNativeOptionChanged -= OnNativeOptionChanged;
			ManagedOptions.OnManagedOptionChanged -= OnManagedOptionChanged;

			base.OnRemoveBehavior();
		}

		private void OnNativeOptionChanged(NativeOptions.NativeOptionsType changedNativeOptionsType)
		{
			if (changedNativeOptionsType == NativeOptions.NativeOptionsType.VoiceOverVolume)
			{
				UpdateVoiceChatEnabled();
				_voipInVolume = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.VoiceOverVolume);
			}
			else if (changedNativeOptionsType == NativeOptions.NativeOptionsType.VoiceChatVolume)
			{
				_voipOutVolume = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.VoiceChatVolume);
			}
		}

		private void HandleOptionsUpdated()
		{
			if (_waveOut != null)
			{
				StopAudioProcessing();
				_waveIn.StopRecording();
				_waveIn.Dispose();
				_waveOut.Stop();
				_waveOut.Dispose();
				_waveIn.DeviceNumber = 0;
				_waveOut.DeviceNumber = 0;
				_waveOut.Init(_waveProvider);
				_waveOut.Init(_audioMixer, false);
				StartAudioProcessing();
				UpdateVoiceChatEnabled();
			}
		}

		private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionType)
		{
			if (changedManagedOptionType == ManagedOptions.ManagedOptionsType.EnableVoiceChat)
			{
				UpdateVoiceChatEnabled();
			}
		}

		private void UpdateVoiceChatEnabled()
		{
			_isVoiceChatDisabled = !BannerlordConfig.EnableVoiceChat || Game.Current.GetGameHandler<ChatBox>().IsContentRestricted;
		}

		/*
         * Handles playing audio
         */
		private void ProcessVoiceFiltering(short[] samples, out byte[] processedBuffer, out int processedBufferLength)
		{
			short[] combinedBuffer = new short[samples.Length];
			samples.CopyTo(combinedBuffer, 0);


			// Loudness protection
			float loudnessThreshold = 16000f; // Adjust the threshold as needed
			float maxAmplitude = combinedBuffer.Max(x => Math.Abs(Math.Max((short)0, x)));

			if (maxAmplitude > loudnessThreshold)
			{
				// Reduce the volume to avoid excessively loud sounds
				float attenuationFactor = loudnessThreshold / maxAmplitude;

				for (int i = 0; i < combinedBuffer.Length; i++)
				{
					combinedBuffer[i] = (short)(combinedBuffer[i] * attenuationFactor);
				}
			}

			// Leaving this commented out for now, we can apply this later if we want
			// Apply the muffler to the agent if he has a full face mask (wonky atm, mostly works)
			//if (Mission.MainAgent != null && Mission.MainAgent.SpawnEquipment.EarsAreHidden)
			//{
			//    _muffleFilter.ApplyMuffle(combinedBuffer, 1.0f, 2.0f);
			//}

			// Convert short array to byte array
			byte[] byteArray = new byte[combinedBuffer.Length * 2];
			Buffer.BlockCopy(combinedBuffer, 0, byteArray, 0, byteArray.Length);

			processedBuffer = byteArray;
			processedBufferLength = byteArray.Length;
		}

		public List<SelectionData> GetAudioOutDevices()
		{
			List<SelectionData> soundDevices = new List<SelectionData>();

			int deviceCount = WaveOut.DeviceCount;
			for (int i = 0; i < deviceCount; i++)
			{
				var deviceInfo = WaveOut.GetCapabilities(i);

				soundDevices.Add(new SelectionData(false, deviceInfo.ProductName));
			}

			return soundDevices;
		}

		public List<SelectionData> GetAudioInDevices()
		{
			List<SelectionData> soundDevices = new List<SelectionData>();

			int deviceCount = WaveIn.DeviceCount;
			for (int i = 0; i < deviceCount; i++)
			{
				var deviceInfo = WaveIn.GetCapabilities(i);

				soundDevices.Add(new SelectionData(false, deviceInfo.ProductName));
			}

			return soundDevices;
		}

		/*
         * Constantly check for nearby agents to add to voice chat
         */
		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);

			if (_isVoiceChatDisabled) { return; }

			CheckNearbyPlayersForVoiceChat();
		}

		/*
         * Voice playback handling
         */
		public override void OnPreDisplayMissionTick(float dt)
		{
			base.OnPreDisplayMissionTick(dt);

			if (GameNetwork.IsServer)
			{
				return;
			}

			int maxPlaybackTicksPerUpdate = 120;
			if (_playedAnyVoicePreviousTick)
			{
				int maxTicksToProcess = MathF.Ceiling(dt * 1000f);
				maxPlaybackTicksPerUpdate = MathF.Min(maxPlaybackTicksPerUpdate, maxTicksToProcess);
				_playedAnyVoicePreviousTick = false;
			}

			foreach (PlayerVoiceData playerVoiceData in _playerVoiceList)
			{
				if (playerVoiceData.Peer != null)
				{
					OnPeerVoiceStatusUpdated?.Invoke(playerVoiceData.Peer, playerVoiceData.HasAnyVoiceData(), false);
				}
				else
				{
					OnBotVoiceStatusUpdated?.Invoke(playerVoiceData.Agent, playerVoiceData.HasAnyVoiceData(), false);
				}
			}

			int totalTicksToProcess = maxPlaybackTicksPerUpdate * 12;
			for (int tickIndex = 0; tickIndex < totalTicksToProcess; tickIndex++)
			{
				for (int playerIndex = 0; playerIndex < _playerVoiceList.Count; playerIndex++)
				{
					_playerVoiceList[playerIndex].ProcessVoiceData();
				}
			}

			int biggestVoiceToPlay = 0;

			for (int k = 0; k < _playerVoiceList.Count; k++)
			{
				Queue<short> voiceToPlayForTick = _playerVoiceList[k].GetVoiceToPlayForTick();

				if (voiceToPlayForTick.Count > 0)
				{
					biggestVoiceToPlay = voiceToPlayForTick.Count() > biggestVoiceToPlay ? voiceToPlayForTick.Count() : biggestVoiceToPlay;

					int count = voiceToPlayForTick.Count;

					byte[] array = new byte[count * 2];
					for (int l = 0; l < count; l++)
					{
						short sample = voiceToPlayForTick.Dequeue();
						byte[] bytes = BitConverter.GetBytes(sample);
						array[l * 2] = bytes[0];
						array[l * 2 + 1] = bytes[1];
					}

					_playerVoiceList[k].UpdateVoice(array, _voipInVolume);

					_playedAnyVoicePreviousTick = true;
				}
			}

			int sampleBufferSize = 1;

			float[] mixedSamples = new float[sampleBufferSize];

			if (IsVoiceRecordActive)
			{
				byte[] voiceBuffer = new byte[VOICE_RECORD_MAX_CHUNK_SIZE_IN_BYTES];


				GetVoiceData(voiceBuffer, VOICE_RECORD_MAX_CHUNK_SIZE_IN_BYTES, out var readBytesLength);


				for (int m = 0; m < readBytesLength; m++)
				{
					_voiceToSend.Enqueue(voiceBuffer[m]);
				}

				CheckStopVoiceRecord();
			}

			while (_voiceToSend.Count > 0 && (_voiceToSend.Count >= VOICE_FRAME_RAW_SIZE_IN_BYTES || !IsVoiceRecordActive))
			{

				int bufferSize = Math.Min(_voiceToSend.Count, VOICE_FRAME_RAW_SIZE_IN_BYTES);
				byte[] numArray = new byte[VOICE_FRAME_RAW_SIZE_IN_BYTES];

				for (int n = 0; n < bufferSize; n++)
				{
					numArray[n] = _voiceToSend.Dequeue();
				}


				WaveBuffer waveBuffer = new WaveBuffer(numArray);

				byte[] compressedData = new byte[COMPRESSION_MAX_CHUNK_SIZE_IN_BYTES];
				int bytesEncoded = _encoder.Encode(waveBuffer.ShortBuffer, 0, OPUS_FRAME_SIZE, compressedData, 0, numArray.Length);


				if (GameNetwork.IsClient)
				{
					GameNetwork.BeginModuleEventAsClientUnreliable();
					GameNetwork.WriteMessage(new ALSendVoiceRecord(compressedData, bytesEncoded, _isAnnouncement));
					GameNetwork.EndModuleEventAsClientUnreliable();
				}
			}

			if (Mission.InputManager.IsKeyPressed(InputKey.F9))
			{
				if (GameNetwork.MyPeer.IsAdmin())
				{
					_isAnnouncement = !_isAnnouncement;
					Log($"{(_isAnnouncement ? "Announcement mode enabled" : "Announcement mode disabled")}", LogLevel.Information);
				}
			}

			if (!IsVoiceRecordActive && Mission.InputManager.IsGameKeyPressed(33))
				IsVoiceRecordActive = true;
			if (!IsVoiceRecordActive || !Mission.InputManager.IsGameKeyReleased(33))
				return;
			_stopRecordingOnNextTick = true;
		}


		// Getter and setter for activating voice recording
		private bool IsVoiceRecordActive
		{
			get
			{
				return _isVoiceRecordActive;
			}
			set
			{
				if (!_isVoiceChatDisabled)
				{
					_isVoiceRecordActive = value;
					if (_isVoiceRecordActive)
					{
						OnVoiceRecordStarted?.Invoke();

						_waveIn.StartRecording();
					}
					else
					{
						OnVoiceRecordStopped?.Invoke();

						_waveIn.StopRecording();
					}
				}
			}
		}

		private void CheckStopVoiceRecord()
		{
			if (_stopRecordingOnNextTick)
			{
				IsVoiceRecordActive = false;
				_stopRecordingOnNextTick = false;
			}
		}

		// We will use this method to override the team behavior. Every MS we will check for nearby players to subscribe to
		private void CheckNearbyPlayersForVoiceChat()
		{
			MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();

			if (missionPeer == null)
			{
				return;
			}

			var peers = GameNetwork.NetworkPeers.ToList();
			Vec3 myPosition = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Position : Mission.GetCameraFrame().origin;

			foreach (NetworkCommunicator iteratedNetworkPeer in peers)
			{
				MissionPeer iteratedMissionPeer = iteratedNetworkPeer.GetComponent<MissionPeer>();
				// Only add players with controlled agents or admins to the voice chat
				if (iteratedNetworkPeer == null || iteratedNetworkPeer.ControlledAgent == null && !iteratedNetworkPeer.IsAdmin())
				{
					continue;
				}

				Vec3 agentPosition;
				if (iteratedNetworkPeer.ControlledAgent != null)
				{
					agentPosition = iteratedNetworkPeer.ControlledAgent.Position;
				}
				else
				{
					// If the other player doesn't control an agent (admin case), we will use our own position
					agentPosition = myPosition;
					agentPosition.z += 1;
				}


				if (AudioHelper.CanTargetHearSound(agentPosition, myPosition))
				{
					int peerVoiceDataIndex = GetPlayerVoiceDataIndex(iteratedMissionPeer);

					if (peerVoiceDataIndex == -1)
					{
						AddPlayerToVoiceChat(iteratedMissionPeer);
					}
				}
				else
				{
					int peerVoiceDataIndex = GetPlayerVoiceDataIndex(iteratedMissionPeer);

					if (peerVoiceDataIndex == -1) { continue; }
					RemovePlayerFromVoiceChat(peerVoiceDataIndex);


					OnPeerVoiceStatusUpdated.Invoke(iteratedMissionPeer, false, true);
				}
			}

			if (!Config.Instance.NoFriend) return;

			foreach (Agent target in Mission.Current.Agents)
			{
				if (target.MissionPeer != null) continue;

				if (AudioHelper.CanTargetHearSound(myPosition, target.Position))
				{
					int agentVoiceDataIndex = _playerVoiceList.FindIndex(x => x.Agent == target);

					if (agentVoiceDataIndex == -1)
					{
						AddBotToVoiceChat(target);
					}
				}
				else
				{
					int agentVoiceDataIndex = _playerVoiceList.FindIndex(x => x.Agent == target);

					if (agentVoiceDataIndex == -1) { continue; }
					RemovePlayerFromVoiceChat(agentVoiceDataIndex);

					OnBotVoiceStatusUpdated.Invoke(target, false, true);
				}
			}
		}

		private void AddBotToVoiceChat(Agent target)
		{
			_playerVoiceList.Add(new PlayerVoiceData(null, _audioMixer, target));
		}

		private void GetVoiceData(byte[] buffer, int bufferSize, out int readBytesLength)
		{
			DequeueVoiceData(buffer, bufferSize, out readBytesLength);
		}

		private void AddPlayerToVoiceChat(MissionPeer missionPeer)
		{
			_playerVoiceList.Add(new PlayerVoiceData(missionPeer, _audioMixer));
		}

		private void RemovePlayerFromVoiceChat(int indexInVoiceDataList)
		{
			PlayerVoiceData playerVoiceData = _playerVoiceList[indexInVoiceDataList];

			playerVoiceData.ClearVoiceData(_audioMixer);

			_playerVoiceList.RemoveAt(indexInVoiceDataList);
		}

		private int GetPlayerVoiceDataIndex(MissionPeer missionPeer)
		{
			for (int i = 0; i < _playerVoiceList.Count; i++)
			{
				if (_playerVoiceList[i].Peer == missionPeer)
				{
					return i;
				}
			}

			return -1;
		}

		private static float CalculateVolume(Vec3 speakerPosition, Vec3 listenerPosition, float distanceCutoff, float maxVolume)
		{
			float distance = speakerPosition.Distance(listenerPosition);

			// Controls the smoothness of the volume change between the 2 positions, higher takes more time for the sound to get louder
			float smoothnessFactor = 5.0f;

			// Exponential function for smoother increase in volume as listener gets closer
			float exponentialVolume = smoothnessFactor * (1.0f - (float)Math.Exp(-distance / distanceCutoff));

			// Scale the exponent to maximum volume
			float volume = maxVolume * MathF.Pow(1.0f - MathF.Clamp(distance / distanceCutoff, 0.0f, 1.0f), exponentialVolume);

			// Limit minimum and maximum volume 
			float minVolume = MathF.Max(0.0f, volume);
			volume = MathF.Min(maxVolume, minVolume);

			return volume;
		}

		public class PlayerVoiceData
		{
			private const int PlayDelaySizeInMilliseconds = 150;
			private const int PlayDelaySizeInBytes = 3600;
			private const float PlayDelayResetTimeInMilliseconds = 300f;

			public readonly MissionPeer Peer;
			public readonly Vec3 Position;
			public readonly Agent Agent;

			private readonly Queue<short> _voiceData;
			private readonly Queue<short> _voiceToPlayInTick;
			private int _playDelayRemainingSizeInBytes;
			private MissionTime _nextPlayDelayResetTime;
			private bool _isSpeaking;

			public WaveOutEvent waveOut { get; private set; }
			public BufferedWaveProvider waveProvider { get; private set; }
			public PanningSampleProvider panProvider { get; private set; }
			public VolumeSampleProvider volumeProvider { get; private set; }

			public bool IsReadyOnPlatform { get; private set; }
			private OpusDecoder _decoder;

			public PlayerVoiceData(MissionPeer peer, MixingSampleProvider mixer, Agent agent = null)
			{
				Peer = peer;
				Agent = agent;
				_voiceData = new Queue<short>();
				_voiceToPlayInTick = new Queue<short>();
				_nextPlayDelayResetTime = MissionTime.Now;
				_decoder = OpusDecoder.Create(SAMPLE_RATE, 1);

				// Initialize NAudio components
				waveProvider = new BufferedWaveProvider(new WaveFormat(SAMPLE_RATE, 16, 1));
				waveProvider.BufferLength = VOICE_RECORD_MAX_CHUNK_SIZE_IN_BYTES;

				volumeProvider = new VolumeSampleProvider(waveProvider.ToSampleProvider());
				panProvider = new PanningSampleProvider(volumeProvider);
				volumeProvider.Volume = 1;

				AddVoiceToMixer(mixer);
			}

			public void WriteVoiceData(short[] voiceData)
			{
				if (_voiceData.Count == 0 && _nextPlayDelayResetTime.IsPast)
				{
					_playDelayRemainingSizeInBytes = PlayDelaySizeInBytes;
				}

				foreach (short data in voiceData)
					_voiceData.Enqueue(data);
			}

			public void SetReadyOnPlatform()
			{
				IsReadyOnPlatform = true;
			}

			public void SetSpeaking(bool isSpeaking)
			{
				_isSpeaking = isSpeaking;
			}

			public bool IsSpeaking()
			{
				return _isSpeaking;
			}

			public bool ProcessVoiceData()
			{
				if (_voiceData.Count > 0)
				{
					bool isMutedFromGameOrPlatform = Peer?.IsMutedFromGameOrPlatform ?? false;
					if (_playDelayRemainingSizeInBytes > 0)
					{
						_playDelayRemainingSizeInBytes -= 2;
					}
					else
					{
						short item = _voiceData.Dequeue();
						_nextPlayDelayResetTime = MissionTime.Now + MissionTime.Milliseconds(PlayDelayResetTimeInMilliseconds);

						if (!isMutedFromGameOrPlatform)
						{
							for (int i = 0; i < _voiceData.Count; i++)
							{
								item = _voiceData.Dequeue();
								_voiceToPlayInTick.Enqueue(item);
							}
						}
					}

					return !isMutedFromGameOrPlatform;
				}

				return false;
			}

			public void ClearVoiceData(MixingSampleProvider audioMixer)
			{
				_voiceData.Clear();
				_voiceToPlayInTick.Clear();
				_nextPlayDelayResetTime = MissionTime.Now;
				_playDelayRemainingSizeInBytes = 0;

				audioMixer.RemoveMixerInput(volumeProvider);
			}

			public Queue<short> GetVoiceToPlayForTick()
			{
				return _voiceToPlayInTick;
			}

			public bool HasAnyVoiceData()
			{
				return _voiceData.Count > 0;
			}

			public int GetVoiceData()
			{
				return _voiceData.Count;
			}

			public OpusDecoder GetDecoder()
			{
				return _decoder;
			}

			public void UpdateVoice(byte[] byteArray, float pbOptionVolume)
			{
				if (Peer == null && Agent == null)
				{
					waveProvider.ClearBuffer();

					return;
				}

				Vec3 speakerPosition;

				if (Peer?.ControlledAgent != null)
				{
					speakerPosition = Peer.ControlledAgent.Position;
				}
				else if (Agent != null)
				{
					speakerPosition = Agent.Position;
				}
				else if (GameNetwork.MyPeer.ControlledAgent != null)
				{
					speakerPosition = GameNetwork.MyPeer.ControlledAgent.Position;
				}
				else
				{
					speakerPosition = Mission.Current.GetCameraFrame().origin;
				}

				Vec3 listenerPosition = GameNetwork.MyPeer.ControlledAgent != null ? GameNetwork.MyPeer.ControlledAgent.Position : Mission.Current.GetCameraFrame().origin;

				// Prevent buffer overflow
				if (waveProvider.BufferedBytes + byteArray.Length > waveProvider.BufferLength)
				{
					waveProvider.ClearBuffer();
				}

				float pbVolume = 5 * pbOptionVolume;

				// Apply panning to the left and right channels
				float clampedVolume = CalculateVolume(speakerPosition, listenerPosition, 30f, pbVolume);

				panProvider.Pan = 0.0f;
				volumeProvider.Volume = clampedVolume;

				if (byteArray.Length <= waveProvider.BufferLength)
				{
					waveProvider.AddSamples(byteArray, 0, byteArray.Length);
				}
			}

			public void AddVoiceToMixer(MixingSampleProvider audioMixer)
			{
				// Check if the audio mixer has too many inputs
				if (audioMixer.MixerInputs.Count() >= 1024)
				{
					// Remove all inputs to prevent audio mixer overflow (might cause audio cut-off)
					audioMixer.RemoveAllMixerInputs();
				}
				audioMixer.AddMixerInput(volumeProvider);

				// Clear the voice data queue
				_voiceToPlayInTick.Clear();
			}
		}

		private class MuffleFilter
		{
			private const float LowPassCutoffFrequency = 2000; // Adjust the cutoff frequency as needed
			private readonly BiQuadFilter lowPassFilter;

			public MuffleFilter(int sampleRate)
			{
				lowPassFilter = BiQuadFilter.LowPassFilter(sampleRate, 600, 0.9071f);
			}

			public void ApplyMuffle(short[] samples, float muffleFactor, float volumeFactor)
			{
				for (int i = 0; i < samples.Length; i++)
				{
					// Apply muffle factor and volume factor to each sample
					samples[i] = (short)(samples[i] * (1 - muffleFactor) + lowPassFilter.Transform(samples[i]) * muffleFactor * volumeFactor);
				}
			}
		}
	}
}
