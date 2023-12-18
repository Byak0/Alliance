using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Extensions.VOIP.Behaviors
{
    /// <summary>
    /// VOIP handler based on Native behavior and NAudio library.
    /// Credits to PersistentBannerlord@2023.
    /// </summary>
    public class VoipHandler : MissionNetwork
    {
        private class PeerVoiceData
        {
            private const int PlayDelaySizeInMilliseconds = 150;

            private const int PlayDelaySizeInBytes = 3600;

            private const float PlayDelayResetTimeInMilliseconds = 300f;

            public readonly MissionPeer Peer;

            private readonly Queue<short> _voiceData;

            private readonly Queue<short> _voiceToPlayInTick;

            private int _playDelayRemainingSizeInBytes;

            private MissionTime _nextPlayDelayResetTime;


            /* 
             * NAudio cooking
             */
            public WaveOutEvent waveOut { get; private set; }
            public BufferedWaveProvider waveProvider { get; private set; }
            public PanningSampleProvider panProvider { get; private set; }
            public VolumeSampleProvider volumeProvider { get; private set; }


            public bool IsReadyOnPlatform { get; private set; }

            public PeerVoiceData(MissionPeer peer)
            {
                Peer = peer;
                _voiceData = new Queue<short>();
                _voiceToPlayInTick = new Queue<short>();
                _nextPlayDelayResetTime = MissionTime.Now;

                waveOut = new WaveOutEvent();

                // Initialize NAudio components
                waveProvider = new BufferedWaveProvider(new WaveFormat(12000, 16, 1));
                waveProvider.BufferLength = VoiceRecordMaxChunkSizeInBytes;
                waveOut.Init(waveProvider);

                volumeProvider = new VolumeSampleProvider(waveProvider.ToSampleProvider());
                panProvider = new PanningSampleProvider(volumeProvider);
                volumeProvider.Volume = 1;

                waveOut.Init(panProvider);
            }

            public void WriteVoiceData(byte[] dataBuffer, int bufferSize)
            {
                if (_voiceData.Count == 0 && _nextPlayDelayResetTime.IsPast)
                {
                    _playDelayRemainingSizeInBytes = PlayDelaySizeInBytes;
                }

                for (int i = 0; i < bufferSize; i += 2)
                {
                    short item = (short)(dataBuffer[i] | dataBuffer[i + 1] << 8);
                    _voiceData.Enqueue(item);
                }
            }

            public void SetReadyOnPlatform()
            {
                IsReadyOnPlatform = true;
            }

            public bool ProcessVoiceData()
            {
                if (IsReadyOnPlatform && _voiceData.Count > 0)
                {
                    bool isMutedFromGameOrPlatform = Peer.IsMutedFromGameOrPlatform;
                    if (_playDelayRemainingSizeInBytes > 0)
                    {
                        _playDelayRemainingSizeInBytes -= 2;
                    }
                    else
                    {
                        short item = _voiceData.Dequeue();
                        _nextPlayDelayResetTime = MissionTime.Now + MissionTime.Milliseconds(300f);
                        if (!isMutedFromGameOrPlatform)
                        {
                            _voiceToPlayInTick.Enqueue(item);
                        }
                    }

                    return !isMutedFromGameOrPlatform;
                }

                return false;
            }

            public Queue<short> GetVoiceToPlayForTick()
            {
                return _voiceToPlayInTick;
            }

            public bool HasAnyVoiceData()
            {
                if (IsReadyOnPlatform)
                {
                    return _voiceData.Count > 0;
                }

                return false;
            }
        }

        private const int MillisecondsToShorts = 12;

        private const int MillisecondsToBytes = 24;

        private const int OpusFrameSizeCoefficient = 6;

        private const int VoiceFrameRawSizeInMilliseconds = 60;

        public const int VoiceFrameRawSizeInBytes = 1440;

        private const int CompressionMaxChunkSizeInBytes = 10640;

        private const int VoiceRecordMaxChunkSizeInBytes = 72000;

        private List<PeerVoiceData> _playerVoiceDataList;

        private bool _isVoiceChatDisabled = true;

        private bool _isVoiceRecordActive;

        private bool _stopRecordingOnNextTick;

        private Queue<byte> _voiceToSend;

        private bool _playedAnyVoicePreviousTick;

        private bool _localUserInitialized;


        /*
         * NAudio Cooking
         */
        private WaveInEvent waveIn;
        private WaveOutEvent waveOut;
        private List<WaveOutEvent> waveOutList;
        private List<BufferedWaveProvider> waveProviderList;

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

        public event Action<MissionPeer> OnPeerMuteStatusUpdated;

        protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            if (GameNetwork.IsClient)
            {
                registerer.RegisterBaseHandler<SendVoiceToPlay>(HandleServerEventSendVoiceToPlay);
            }
            else if (GameNetwork.IsServer)
            {
                registerer.RegisterBaseHandler<SendVoiceRecord>(HandleClientEventSendVoiceRecord);
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            if (!GameNetwork.IsDedicatedServer)
            {
                _playerVoiceDataList = new List<PeerVoiceData>();
                SoundManager.InitializeVoicePlayEvent();
                _voiceToSend = new Queue<byte>();
            }
        }

        public override void AfterStart()
        {
            UpdateVoiceChatEnabled();
            if (!_isVoiceChatDisabled)
            {
                MissionPeer.OnTeamChanged += MissionPeerOnTeamChanged;
                Mission.Current.GetMissionBehavior<MissionNetworkComponent>().OnClientSynchronizedEvent += OnPlayerSynchronized;
            }

            NativeOptions.OnNativeOptionChanged = (NativeOptions.OnNativeOptionChangedDelegate)Delegate.Combine(NativeOptions.OnNativeOptionChanged, new NativeOptions.OnNativeOptionChangedDelegate(OnNativeOptionChanged));
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
        }

        public override void OnRemoveBehavior()
        {
            if (!_isVoiceChatDisabled)
            {
                MissionPeer.OnTeamChanged -= MissionPeerOnTeamChanged;
            }

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
                VoiceTick(dt);
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (_playerVoiceDataList == null)
            {
                return;
            }

            CheckNearbyPlayersForVoiceChat();
        }


        private bool HandleClientEventSendVoiceRecord(NetworkCommunicator peer, GameNetworkMessage baseMessage)
        {
            SendVoiceRecord sendVoiceRecord = (SendVoiceRecord)baseMessage;
            MissionPeer component = peer.GetComponent<MissionPeer>();

            if (component == null)
            {
                return false;
            }

            if (peer.ControlledAgent == null)
            {
                return false;
            }

            if (sendVoiceRecord.BufferLength > 0 && component.Team != null)
            {
                foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                {
                    MissionPeer component2 = networkPeer.GetComponent<MissionPeer>();

                    if (component2 == null)
                    {
                        continue;
                    }

                    if (component2.ControlledAgent == null)
                    {
                        continue;
                    }

                    float distance = component2.ControlledAgent.Position.Distance(peer.ControlledAgent.Position);

                    if (networkPeer.IsSynchronized && component2 != null && distance < 30 && (sendVoiceRecord.ReceiverList == null || sendVoiceRecord.ReceiverList.Contains(networkPeer.VirtualPlayer)) && component2 != component)
                    {
                        GameNetwork.BeginModuleEventAsServerUnreliable(component2.Peer);
                        GameNetwork.WriteMessage(new SendVoiceToPlay(peer, sendVoiceRecord.Buffer, sendVoiceRecord.BufferLength));
                        GameNetwork.EndModuleEventAsServerUnreliable();
                    }
                }
            }

            return true;
        }

        private void HandleServerEventSendVoiceToPlay(GameNetworkMessage baseMessage)
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

            for (int i = 0; i < _playerVoiceDataList.Count; i++)
            {
                if (_playerVoiceDataList[i].Peer == component)
                {
                    byte[] voiceBuffer = new byte[CompressionMaxChunkSizeInBytes];
                    DecompressVoiceChunk(sendVoiceToPlay.Peer.Index, sendVoiceToPlay.Buffer, sendVoiceToPlay.BufferLength, ref voiceBuffer, out var bufferLength);
                    _playerVoiceDataList[i].WriteVoiceData(voiceBuffer, bufferLength);
                    break;
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

        private void VoiceTick(float dt)
        {
            int num = 120;
            if (_playedAnyVoicePreviousTick)
            {
                int b = MathF.Ceiling(dt * 1000f);
                num = MathF.Min(num, b);
                _playedAnyVoicePreviousTick = false;
            }

            foreach (PeerVoiceData playerVoiceData in _playerVoiceDataList)
            {
                OnPeerVoiceStatusUpdated?.Invoke(playerVoiceData.Peer, playerVoiceData.HasAnyVoiceData());
            }

            int num2 = num * 12;
            for (int i = 0; i < num2; i++)
            {
                for (int j = 0; j < _playerVoiceDataList.Count; j++)
                {
                    _playerVoiceDataList[j].ProcessVoiceData();
                }
            }

            for (int k = 0; k < _playerVoiceDataList.Count; k++)
            {
                Queue<short> voiceToPlayForTick = _playerVoiceDataList[k].GetVoiceToPlayForTick();

                Agent speakerAgent = _playerVoiceDataList[k].Peer.ControlledAgent;
                if (voiceToPlayForTick.Count > 0)
                {
                    int count = voiceToPlayForTick.Count;
                    byte[] array = new byte[count * 2];
                    for (int l = 0; l < count; l++)
                    {
                        byte[] bytes = BitConverter.GetBytes(voiceToPlayForTick.Dequeue());
                        array[l * 2] = bytes[0];
                        array[l * 2 + 1] = bytes[1];
                    }


                    if (speakerAgent == null)
                    {
                        _playerVoiceDataList[k].waveProvider.ClearBuffer();

                        return;
                    }

                    Vec3 speakerPosition = speakerAgent.Position;

                    if (GameNetwork.MyPeer.ControlledAgent == null)
                    {
                        _playerVoiceDataList[k].waveProvider.ClearBuffer();

                        return;
                    }


                    Vec3 listenerPosition = GameNetwork.MyPeer.ControlledAgent.Position;

                    Mat3 listenerRotation = GameNetwork.MyPeer.ControlledAgent.LookRotation;
                    SoundEventParameter soundEventParam = new SoundEventParameter();

                    // Calculate the position difference between the speaker and the listener
                    float pan = CalculatePan(speakerPosition, listenerPosition, listenerRotation);

                    if (_playerVoiceDataList[k].waveProvider.BufferedBytes + VoiceFrameRawSizeInBytes > _playerVoiceDataList[k].waveProvider.BufferLength)
                    {
                        _playerVoiceDataList[k].waveProvider.ClearBuffer();
                    }

                    // Apply panning to the left and right channels
                    float clampedVolume = CalculateVolume(speakerPosition, listenerPosition, 20f);

                    _playerVoiceDataList[k].waveProvider.AddSamples(array, 0, array.Length);
                    _playerVoiceDataList[k].panProvider.Pan = -pan;
                    _playerVoiceDataList[k].volumeProvider.Volume = clampedVolume;

                    _playerVoiceDataList[k].waveOut.Play();
                    /*waveOut.3D
                    waveOut.Volume = 1;*/


                    //SoundManager.UpdateVoiceToPlay(array, array.Length, k);

                    _playedAnyVoicePreviousTick = true;
                }
            }

            if (IsVoiceRecordActive)
            {
                byte[] array2 = new byte[VoiceRecordMaxChunkSizeInBytes];
                SoundManager.GetVoiceData(array2, VoiceRecordMaxChunkSizeInBytes, out var readBytesLength);
                for (int m = 0; m < readBytesLength; m++)
                {
                    _voiceToSend.Enqueue(array2[m]);
                }

                CheckStopVoiceRecord();
            }

            while (_voiceToSend.Count > 0 && (_voiceToSend.Count >= VoiceFrameRawSizeInBytes || !IsVoiceRecordActive))
            {
                int num3 = MathF.Min(_voiceToSend.Count, VoiceFrameRawSizeInBytes);
                byte[] array3 = new byte[VoiceFrameRawSizeInBytes];
                for (int n = 0; n < num3; n++)
                {
                    array3[n] = _voiceToSend.Dequeue();
                }

                if (GameNetwork.IsClient)
                {
                    byte[] compressedBuffer = new byte[CompressionMaxChunkSizeInBytes];
                    CompressVoiceChunk(0, array3, ref compressedBuffer, out var compressedBufferLength);
                    GameNetwork.BeginModuleEventAsClientUnreliable();
                    GameNetwork.WriteMessage(new SendVoiceRecord(compressedBuffer, compressedBufferLength));
                    GameNetwork.EndModuleEventAsClientUnreliable();
                }
                else
                {
                    if (!GameNetwork.IsServer)
                    {
                        continue;
                    }

                    MissionPeer myMissionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
                    if (myMissionPeer != null)
                    {
                        _playerVoiceDataList.Single((x) => x.Peer == myMissionPeer).WriteVoiceData(array3, num3);
                    }
                }
            }

            if (!IsVoiceRecordActive && Mission.InputManager.IsGameKeyPressed(33))
            {
                IsVoiceRecordActive = true;
            }

            if (IsVoiceRecordActive && Mission.InputManager.IsGameKeyReleased(33))
            {
                _stopRecordingOnNextTick = true;
            }
        }

        private float CalculateVolume(Vec3 speakerPosition, Vec3 listenerPosition, float distanceCutoff)
        {
            // Calculate the distance between the speaker and the listener
            float distance = speakerPosition.Distance(listenerPosition);

            // Adjust volume based on distance
            float volume = 1.0f - MathF.Clamp(distance / distanceCutoff, 0.0f, 1.0f);

            return volume;
        }

        private float CalculatePan(Vec3 speakerPosition, Vec3 listenerPosition, Mat3 listenerRotation)
        {
            // Calculate the vector from speaker to listener
            Vec3 speakerToListener = listenerPosition - speakerPosition;

            // Invert the listener's rotation matrix to get the listener's forward vector in world space
            Mat3 invertedRotation = listenerRotation.Transpose(); // Note: Inversion for rotation matrices is equivalent to transpose
            Vec3 listenerForward = invertedRotation.TransformToParent(Vec3.Forward);

            // Project the speaker-to-listener vector onto the plane defined by the listener's forward vector
            Vec3 projectedVector = speakerToListener - Vec3.DotProduct(speakerToListener, listenerForward) * listenerForward;

            // Calculate the signed angle between the projected vector and the listener's right vector
            float angle = MathF.Atan2(Vec3.DotProduct(projectedVector, listenerRotation.s), Vec3.DotProduct(projectedVector, listenerRotation.u));

            // Normalize the angle to the range [-π, π]
            angle = (angle + MathF.PI) % (2 * MathF.PI) - MathF.PI;

            // Normalize the angle to the range [-1.0, 1.0]
            float pan = angle / MathF.PI;

            // Clamp pan value to the valid range [-1.0, 1.0]
            return MathF.Clamp(pan, -1.0f, 1.0f);
        }


        // We will use this method to override the team behavior. Every MS we will check for nearby players to subscribe to
        private void CheckNearbyPlayersForVoiceChat()
        {

            MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();

            if (missionPeer == null && GameNetwork.IsClient)
            {
                return;
            }

            if ((!_localUserInitialized || missionPeer.ControlledAgent == null) && GameNetwork.IsClient)
            {
                return;
            }

            var peers = GameNetwork.NetworkPeers.ToList();

            foreach (NetworkCommunicator iteratedNetworkPeer in peers)
            {
                if (iteratedNetworkPeer.ControlledAgent == null)
                {
                    continue;
                }

                MissionPeer iteratedMissionPeer = iteratedNetworkPeer.ControlledAgent.MissionPeer;

                if (iteratedNetworkPeer.ControlledAgent.MissionPeer == null)
                {
                    continue;
                }

                Vec3 agentPosition = iteratedNetworkPeer.ControlledAgent.Position;

                NetworkCommunicator foundNearbyPlayer = peers.FirstOrDefault(peer => peer.ControlledAgent != null && peer.ControlledAgent.Position.Distance(agentPosition) < 30);

                Vec3 distanceComparator = GameNetwork.IsClient ? GameNetwork.MyPeer.ControlledAgent.Position : foundNearbyPlayer.ControlledAgent.Position;

                if (agentPosition.Distance(distanceComparator) < 30)
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
                }
            }
        }

        /*
         * NAudio Cooking Stop
         */

        private void DecompressVoiceChunk(int clientID, byte[] compressedVoiceBuffer, int compressedBufferLength, ref byte[] voiceBuffer, out int bufferLength)
        {
            SoundManager.DecompressData(clientID, compressedVoiceBuffer, compressedBufferLength, voiceBuffer, out bufferLength);
        }

        private void CompressVoiceChunk(int clientIndex, byte[] voiceBuffer, ref byte[] compressedBuffer, out int compressedBufferLength)
        {
            SoundManager.CompressData(clientIndex, voiceBuffer, VoiceFrameRawSizeInBytes, compressedBuffer, out compressedBufferLength);
        }

        private PeerVoiceData GetPlayerVoiceData(MissionPeer missionPeer)
        {
            for (int i = 0; i < _playerVoiceDataList.Count; i++)
            {
                if (_playerVoiceDataList[i].Peer == missionPeer)
                {
                    return _playerVoiceDataList[i];
                }
            }

            return null;
        }

        private int GetPlayerVoiceDataIndex(MissionPeer missionPeer)
        {
            for (int i = 0; i < _playerVoiceDataList.Count; i++)
            {
                if (_playerVoiceDataList[i].Peer == missionPeer)
                {
                    return i;
                }
            }

            return -1;
        }

        private void AddPlayerToVoiceChat(MissionPeer missionPeer)
        {
            VirtualPlayer peer = missionPeer.Peer;
            _playerVoiceDataList.Add(new PeerVoiceData(missionPeer));
            SoundManager.CreateVoiceEvent();
            PlatformServices.Instance.CheckPermissionWithUser(Permission.CommunicateUsingVoice, missionPeer.Peer.Id, delegate (bool hasPermission)
            {
                if (Mission.Current != null && Mission.Current.CurrentState == Mission.State.Continuing)
                {
                    PeerVoiceData playerVoiceData = GetPlayerVoiceData(missionPeer);
                    if (playerVoiceData != null)
                    {
                        if (!hasPermission && missionPeer.Peer.Id.ProvidedType == NetworkMain.GameClient?.PlayerID.ProvidedType)
                        {
                            missionPeer.SetMutedFromPlatform(isMuted: true);
                        }

                        playerVoiceData.SetReadyOnPlatform();
                    }
                }
            });
            missionPeer.SetMuted(PermaMuteList.IsPlayerMuted(missionPeer.Peer.Id));
            SoundManager.AddSoundClientWithId((ulong)peer.Index);
            OnPeerMuteStatusUpdated?.Invoke(missionPeer);
        }

        private void RemovePlayerFromVoiceChat(int indexInVoiceDataList)
        {
            _ = _playerVoiceDataList[indexInVoiceDataList].Peer.Peer;
            SoundManager.DeleteSoundClientWithId((ulong)_playerVoiceDataList[indexInVoiceDataList].Peer.Peer.Index);
            SoundManager.DestroyVoiceEvent(indexInVoiceDataList);
            _playerVoiceDataList.RemoveAt(indexInVoiceDataList);

            // NAudio Cooking

        }

        private void MissionPeerOnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
        {
            if (_localUserInitialized && peer.VirtualPlayer.Id != PlayerId.Empty)
            {
                //CheckPlayerForVoiceChatOnTeamChange(peer, previousTeam, newTeam);
            }
        }

        private void OnPlayerSynchronized(NetworkCommunicator networkPeer)
        {
            if (_localUserInitialized)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (!component.IsMine && component.Team != null)
                {
                    //CheckPlayerForVoiceChatOnTeamChange(networkPeer, null, component.Team);
                }
            }
            else if (networkPeer.IsMine)
            {
                MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
                //CheckPlayerForVoiceChatOnTeamChange(GameNetwork.MyPeer, null, missionPeer.Team);

                // Init local player to true
                _localUserInitialized = true;
            }
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

        public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
        {
            base.OnPlayerDisconnectedFromServer(networkPeer);
            MissionPeer missionPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
            if (component?.Team == null || missionPeer?.Team == null)
            {
                return;
            }

            for (int i = 0; i < _playerVoiceDataList.Count; i++)
            {
                if (_playerVoiceDataList[i].Peer == component)
                {
                    RemovePlayerFromVoiceChat(i);
                    break;
                }
            }
        }
    }
}