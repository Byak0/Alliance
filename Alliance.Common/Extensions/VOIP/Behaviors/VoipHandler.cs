using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.VOIP.Models;
using Alliance.Common.Extensions.VOIP.NetworkMessages;
using Alliance.Common.Extensions.VOIP.Utilities;
using Concentus.Enums;
using Concentus.Structs;
using NAudio.Wave;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.VOIP.Behaviors
{
    /// <summary>
    /// VOIP handler based on Native behavior and NAudio library.
    /// Credits to PersistentBannerlord@2023.
    /// </summary>
    public class VoipHandler : MissionNetwork
    {
        private VoiceDataManager _speakerList;
        private bool _isVoiceChatDisabled = true;
        private bool _isVoiceRecordActive;
        private bool _stopRecordingOnNextTick;
        private Queue<byte> _voiceToSend;
        private OpusEncoder _encoder;

        // Keep track of the list of speakers every player can hear, so we can limit it.
        private Dictionary<VirtualPlayer, VoiceDataManager> playersSpeakers = new Dictionary<VirtualPlayer, VoiceDataManager>();

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
                        SoundManager.StartVoiceRecording();
                        OnVoiceRecordStarted?.Invoke();
                    }
                    else
                    {
                        SoundManager.StopVoiceRecording();
                        OnVoiceRecordStopped?.Invoke();
                    }
                }
            }
        }

        public event Action OnVoiceRecordStarted;
        public event Action OnVoiceRecordStopped;
        public event Action<MissionPeer, bool> OnPeerVoiceStatusUpdated;
        public event Action<Agent, bool> OnBotVoiceStatusUpdated;
        public event Action<MissionPeer> OnPeerMuteStatusUpdated;

        protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            if (GameNetwork.IsClient)
            {
                registerer.RegisterBaseHandler<SendVoiceToPlay>(HandleServerEventSendVoiceToPlay);
                registerer.RegisterBaseHandler<SendBotVoiceToPlay>(HandleServerEventSendBotVoiceToPlay);
            }
            else if (GameNetwork.IsServer)
            {
                registerer.RegisterBaseHandler<SendVoiceRecord>(HandleClientEventSendVoiceRecord);
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            if (GameNetwork.IsDedicatedServer)
            {
                foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                {
                    if (!playersSpeakers.TryGetValue(networkPeer.VirtualPlayer, out VoiceDataManager speakerList))
                    {
                        speakerList = new VoiceDataManager(networkPeer);
                        playersSpeakers.Add(networkPeer.VirtualPlayer, speakerList);
                    }
                }
            }
            else
            {
                SoundManager.InitializeVoicePlayEvent();
                _voiceToSend = new Queue<byte>();
                _speakerList = new VoiceDataManager(GameNetwork.MyPeer);
                _encoder = new OpusEncoder(VoipConstants.SampleRate, VoipConstants.Channels, OpusApplication.OPUS_APPLICATION_AUDIO);
                _encoder.Bitrate = 12000;
            }
            SoundManager.AddSoundClientWithId(0);
        }

        public override void AfterStart()
        {
            UpdateVoiceChatEnabled();

            NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Combine(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
        }

        public override void OnRemoveBehavior()
        {
            if (!GameNetwork.IsDedicatedServer)
            {
                if (IsVoiceRecordActive)
                {
                    IsVoiceRecordActive = false;
                }

                SoundManager.FinalizeVoicePlayEvent();
            }

            NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Remove(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
            base.OnRemoveBehavior();
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            if (!GameNetwork.IsDedicatedServer && !_isVoiceChatDisabled)
            {
                CheckPlayerVoice();
                _speakerList.PlayAll();
            }
        }

        public override void OnPlayerConnectedToServer(NetworkCommunicator networkPeer)
        {
            if (!playersSpeakers.TryGetValue(networkPeer.VirtualPlayer, out VoiceDataManager speakerList))
            {
                speakerList = new VoiceDataManager(networkPeer);
                playersSpeakers.Add(networkPeer.VirtualPlayer, speakerList);
            }
        }

        public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
        {
            playersSpeakers.Remove(networkPeer.VirtualPlayer);
        }

        /// <summary>
        /// Server side - Handle voice records sent by clients.
        /// </summary>
        private bool HandleClientEventSendVoiceRecord(NetworkCommunicator speaker, GameNetworkMessage message)
        {
            SendVoiceRecord sendVoiceRecord = (SendVoiceRecord)message;
            MissionPeer emitterPeer = speaker.GetComponent<MissionPeer>();
            Agent speakerAgent = emitterPeer?.ControlledAgent;

            if (speakerAgent == null)
            {
                return false;
            }

            if (sendVoiceRecord.BufferLength > 0)
            {
                foreach (Agent target in Mission.Current.Agents)
                {
                    MissionPeer player = target.MissionPeer;

                    if (speakerAgent != target && VoipHelper.CanTargetHearVoice(speakerAgent.Position, target.Position))
                    {
                        if (player != null && playersSpeakers.TryGetValue(player.Peer, out VoiceDataManager speakerList))
                        {
                            speakerList.CleanupExpiredVoices();
                            if (speakerList.TryHearing(speaker))
                            {
                                speakerList.UpdateVoiceData(speaker, sendVoiceRecord.Buffer, sendVoiceRecord.BufferLength);
                            }
                        }
                        else if (Config.Instance.NoFriend)
                        {
                            // For test purpose, make bot repeat what player said
                            MakeBotTalk(target, sendVoiceRecord.Buffer, sendVoiceRecord.BufferLength, 1, 1);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Test method for when you have no friends.
        /// </summary>
        private void MakeBotTalk(Agent speaker, byte[] buffer, int bufferLength, int nbRepeat = 1, float delay = 1f)
        {
            if (speaker != null && nbRepeat > 0)
            {
                Log($"Bot {speaker.Name} is talking (repeat={nbRepeat})", LogLevel.Debug);
                BotTalk(speaker, buffer, bufferLength, nbRepeat, delay);
            }
        }

        // For test purpose.
        private void BotTalk(Agent speaker, byte[] buffer, int bufferLength, int nbRepeat, float delay)
        {
            foreach (Agent target in Mission.Current.Agents.ToMBList())
            {
                MissionPeer player = target.MissionPeer;

                if (speaker != target && VoipHelper.CanTargetHearVoice(speaker.Position, target.Position))
                {
                    // If target is player, send him the record
                    if (player != null && playersSpeakers.TryGetValue(player.Peer, out VoiceDataManager speakerList))
                    {
                        speakerList.CleanupExpiredVoices();
                        if (speakerList.TryHearing(speaker))
                        {
                            speakerList.UpdateVoiceData(speaker, buffer, bufferLength);
                        }
                    }
                    // Else if target is bot, have him repeat the record
                    else
                    {
                        MakeBotTalk(target, buffer, bufferLength, nbRepeat - 1, delay);
                    }
                }
            }
        }

        /// <summary>
        /// Client side - Handle voice records sent by server.
        /// </summary>
        private void HandleServerEventSendVoiceToPlay(GameNetworkMessage message)
        {
            SendVoiceToPlay sendVoiceToPlay = (SendVoiceToPlay)message;
            if (_isVoiceChatDisabled)
            {
                return;
            }

            MissionPeer emitterPeer = sendVoiceToPlay.Peer.GetComponent<MissionPeer>();
            if (emitterPeer == null || sendVoiceToPlay.BufferLength <= 0 || emitterPeer.IsMutedFromGameOrPlatform)
            {
                return;
            }

            _speakerList.CleanupExpiredVoices();
            UpdatePeerVoiceData(emitterPeer, sendVoiceToPlay.Buffer, sendVoiceToPlay.BufferLength);
            OnPeerVoiceStatusUpdated?.Invoke(emitterPeer, !sendVoiceToPlay.Buffer.IsEmpty());
        }

        /// <summary>
        /// Client side - Handle bot voice records sent by server.
        /// </summary>
        private void HandleServerEventSendBotVoiceToPlay(GameNetworkMessage message)
        {
            SendBotVoiceToPlay sendBotVoiceToPlay = (SendBotVoiceToPlay)message;
            if (_isVoiceChatDisabled)
            {
                return;
            }

            _speakerList.CleanupExpiredVoices();
            UpdateBotVoiceData(sendBotVoiceToPlay.Agent, sendBotVoiceToPlay.Buffer, sendBotVoiceToPlay.BufferLength);
            OnBotVoiceStatusUpdated?.Invoke(sendBotVoiceToPlay.Agent, !sendBotVoiceToPlay.Buffer.IsEmpty());
        }

        private void UpdatePeerVoiceData(MissionPeer peer, byte[] compressedBuffer, int compressedBufferLength)
        {
            if (_speakerList.TryHearing(peer.GetNetworkPeer()))
            {
                _speakerList.UpdateVoiceData(peer.GetNetworkPeer(), compressedBuffer, compressedBufferLength);
            }
        }

        // For test purpose.
        private void UpdateBotVoiceData(Agent agent, byte[] compressedBuffer, int compressedBufferLength)
        {
            if (_speakerList.TryHearing(agent))
            {
                _speakerList.UpdateVoiceData(agent, compressedBuffer, compressedBufferLength);
            }
        }

        private void CheckPlayerVoice()
        {
            // Check if voice recording is active
            if (IsVoiceRecordActive)
            {
                // Buffer to store recorded voice data
                byte[] voiceDataBuffer = new byte[VoipConstants.VoiceRecordMaxChunkSizeInBytes];

                // Get the recorded voice data
                SoundManager.GetVoiceData(voiceDataBuffer, VoipConstants.VoiceRecordMaxChunkSizeInBytes, out var readBytesLength);

                // Enqueue the recorded data for sending
                for (int i = 0; i < readBytesLength; i++)
                {
                    _voiceToSend.Enqueue(voiceDataBuffer[i]);
                }

                // Check if recording should be stopped on the next tick
                if (_stopRecordingOnNextTick)
                {
                    IsVoiceRecordActive = false;
                    _stopRecordingOnNextTick = false;
                }
            }

            // Process the voice data queue
            while (_voiceToSend.Count > 0 && (_voiceToSend.Count >= VoipConstants.VoiceFrameRawSizeInBytes || !IsVoiceRecordActive))
            {
                // Determine the size of the data chunk to process
                int chunkSize = MathF.Min(_voiceToSend.Count, VoipConstants.VoiceFrameRawSizeInBytes);
                byte[] voiceChunk = new byte[VoipConstants.VoiceFrameRawSizeInBytes]; // Always use a full frame size

                // Dequeue the voice data chunk
                int i;
                for (i = 0; i < chunkSize; i++)
                {
                    voiceChunk[i] = _voiceToSend.Dequeue();
                }

                // Compression
                byte[] compressedBuffer = new byte[VoipConstants.CompressionMaxChunkSizeInBytes];
                CompressVoiceChunk(voiceChunk, ref compressedBuffer, out var compressedBufferLength);

                GameNetwork.BeginModuleEventAsClientUnreliable();
                GameNetwork.WriteMessage(new SendVoiceRecord(compressedBuffer, compressedBufferLength));
                GameNetwork.EndModuleEventAsClientUnreliable();
            }

            // Toggle voice recording based on input
            if (!IsVoiceRecordActive && Mission.InputManager.IsGameKeyPressed(33))
            {
                IsVoiceRecordActive = true;
            }
            else if (IsVoiceRecordActive && Mission.InputManager.IsGameKeyReleased(33))
            {
                _stopRecordingOnNextTick = true;
            }
        }

        private void CompressVoiceChunk(byte[] voiceBuffer, ref byte[] compressedBuffer, out int compressedBufferLength)
        {
            WaveBuffer waveBuffer = new WaveBuffer(voiceBuffer);
            compressedBufferLength = _encoder.Encode(waveBuffer.ShortBuffer, 0, VoipConstants.FrameSize, compressedBuffer, 0, voiceBuffer.Length); // this throws OpusException on a failure, rather than returning a negative number
        }

        private void UpdateVoiceChatEnabled()
        {
            float num = 1f;
            _isVoiceChatDisabled = !BannerlordConfig.EnableVoiceChat || num <= 1E-05f || Game.Current.GetGameHandler<ChatBox>().IsContentRestricted;
        }

        private void OnNativeOptionChanged(NativeOptions.NativeOptionsType changedNativeOptionsType)
        {
            if (changedNativeOptionsType == NativeOptions.NativeOptionsType.VoiceChatVolume)
            {
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
    }
}