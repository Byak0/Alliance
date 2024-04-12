using NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.VOIP.NetworkMessages.FromClient;
using static Alliance.Common.Utilities.Logger;
using Alliance.Common.Extensions.VOIP.NetworkMessages.FromServer;
using Alliance.Common.Extensions.RTSCamera.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Audio.Utilities;

namespace Alliance.Server.Extensions.VOIP.Behaviors
{
    /// <summary>
    /// VOIP system made by the Persistent Bannerlord team.
    /// Credits to PersistentBannerlord@2023.
    /// </summary>
    public class PBVoiceChatHandler : IHandlerRegister
    {
        // Sample rate is 44100 samples per second. The buffer size contains about 0.18 seconds of data

        private const int VoiceRecordMaxChunkSizeInBytes = 72000;
        public const int VoiceFrameRawSizeInBytes = 1440;
        private const int CompressionMaxChunkSizeInBytes = 8640;

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.RegisterBaseHandler<ALSendVoiceRecord>(HandleClientEventSendVoiceRecord);
        }

        private bool HandleClientEventSendVoiceRecord(NetworkCommunicator speaker, GameNetworkMessage baseMessage)
        {
            ALSendVoiceRecord sendVoiceRecord = (ALSendVoiceRecord)baseMessage;
            MissionPeer speakerPeer = speaker.GetComponent<MissionPeer>();

            if (speakerPeer?.Team == null || sendVoiceRecord.BufferLength <= 0)
            {
                return false;
            }

            Vec3 speakerPosition;
            if (sendVoiceRecord.IsAnnouncement && speaker.IsAdmin())
            {
                speakerPosition = speaker.GetCameraPosition();
                Log($"Speaker {speaker.UserName} camera position = {speakerPosition}", LogLevel.Debug);
            }
            else if(speaker.ControlledAgent != null)
            {
                speakerPosition = speaker.ControlledAgent.Position;
            } 
            else 
            {
                return false;
            }

            List<NetworkCommunicator> agentList = GameNetwork.NetworkPeers.ToList();

            foreach (NetworkCommunicator listener in agentList)
            {
                MissionPeer listenerPeer = listener.GetComponent<MissionPeer>();

                if(!listener.IsSynchronized || listenerPeer == null || listenerPeer == speakerPeer)
                {
                    continue;
                }

                Vec3 listenerPosition;
                if (listenerPeer.ControlledAgent != null)
                {
                    listenerPosition = listenerPeer.ControlledAgent.Position;
                }
                else
                {
                    listenerPosition = listener.GetCameraPosition();
                    Log($"Listener {listener.UserName} camera position = {listenerPosition}", LogLevel.Debug);
                }

                if (AudioHelper.CanTargetHearSound(speakerPosition, listenerPosition) || sendVoiceRecord.IsAnnouncement)
                {
                    GameNetwork.BeginModuleEventAsServerUnreliable(listenerPeer.Peer);
                    GameNetwork.WriteMessage(new SendVoiceToPlay(speaker, sendVoiceRecord.Buffer, sendVoiceRecord.BufferLength));
                    GameNetwork.EndModuleEventAsServerUnreliable();
                }
            }

            if (Config.Instance.NoFriend)
            {
                foreach (Agent target in Mission.Current.AllAgents)
                {
                    if (target.MissionPeer != null) continue;

                    if (AudioHelper.CanTargetHearSound(speakerPosition, target.Position) || sendVoiceRecord.IsAnnouncement)
                    {
                        // For test purpose, make bot repeat what player said
                        MakeBotTalk(target, sendVoiceRecord.Buffer, sendVoiceRecord.BufferLength, 1, 1);
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
            foreach (NetworkCommunicator listener in GameNetwork.NetworkPeers)
            {
                MissionPeer listenerPeer = listener.GetComponent<MissionPeer>();

                if (!listener.IsSynchronized || listenerPeer == null)
                {
                    continue;
                }

                Vec3 listenerPosition;
                if (listenerPeer.ControlledAgent != null)
                {
                    listenerPosition = listenerPeer.ControlledAgent.Position;
                }
                else
                {
                    listenerPosition = listener.GetCameraPosition();
                    Log($"Listener {listener.UserName} camera position = {listenerPosition}", LogLevel.Debug);
                }

                if (AudioHelper.CanTargetHearSound(speaker.Position, listenerPosition))
                {
                    GameNetwork.BeginModuleEventAsServerUnreliable(listenerPeer.Peer);
                    GameNetwork.WriteMessage(new SendBotVoiceToPlay(speaker, buffer, bufferLength));
                    GameNetwork.EndModuleEventAsServerUnreliable();
                }
            }

            foreach (Agent target in Mission.Current.Agents)
            {
                if (target.MissionPeer != null) continue;

                if (speaker != target && AudioHelper.CanTargetHearSound(speaker.Position, target.Position))
                {
                    // For test purpose, make bot repeat what player said
                    MakeBotTalk(target, buffer, bufferLength, nbRepeat - 1, delay);
                }
            }
        }
    }
}
