using Alliance.Common.Extensions.VOIP.Models;
using Alliance.Common.Extensions.VOIP.NetworkMessages;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.VOIP.Behaviors
{
    /// <summary>
    /// Manage the list of Voice Data available to a player (the listener).
    /// </summary>
    public class VoiceDataManager
    {
        public NetworkCommunicator Listener { get; private set; }
        private List<VoiceData> voiceDataList;

        public VoiceDataManager(NetworkCommunicator listener)
        {
            Listener = listener;
            voiceDataList = new List<VoiceData>();
        }

        public bool TryHearing(NetworkCommunicator peer)
        {
            PeerVoiceData peerVoiceData = (PeerVoiceData)voiceDataList.FirstOrDefault(vd => vd is PeerVoiceData peerVoiceData && peerVoiceData.Peer == peer);
            if (peerVoiceData == null)
            {
                if (IsFull())
                {
                    return false;
                }
                peerVoiceData = new PeerVoiceData(peer, VoipConstants.SAMPLE_RATE, VoipConstants.CHANNELS);
                voiceDataList.Add(peerVoiceData);
            }
            return true;
        }

        // For test purpose.
        public bool TryHearing(Agent agent)
        {
            BotVoiceData botVoiceData = (BotVoiceData)voiceDataList.FirstOrDefault(vd => vd is BotVoiceData peerVoiceData && peerVoiceData.Agent.Index == agent.Index);
            if (botVoiceData == null)
            {
                if (IsFull())
                {
                    return false;
                }
                botVoiceData = new BotVoiceData(agent, VoipConstants.SAMPLE_RATE, VoipConstants.CHANNELS);
                voiceDataList.Add(botVoiceData);
            }
            return true;
        }

        public void UpdateVoiceData(NetworkCommunicator peer, byte[] dataBuffer, int bufferSize)
        {
            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginModuleEventAsServerUnreliable(Listener);
                GameNetwork.WriteMessage(new SendVoiceToPlay(peer, dataBuffer, bufferSize));
                GameNetwork.EndModuleEventAsServerUnreliable();
            }
            else
            {
                PeerVoiceData peerVoiceData = (PeerVoiceData)voiceDataList.FirstOrDefault(vd => vd is PeerVoiceData peerVoiceData && peerVoiceData.Peer == peer);
                peerVoiceData?.UpdateVoiceData(dataBuffer, bufferSize);
            }
        }

        // For test purpose.
        public void UpdateVoiceData(Agent agent, byte[] dataBuffer, int bufferSize)
        {
            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginModuleEventAsServerUnreliable(Listener);
                GameNetwork.WriteMessage(new SendBotVoiceToPlay(agent, dataBuffer, bufferSize));
                GameNetwork.EndModuleEventAsServerUnreliable();
            }
            else
            {
                BotVoiceData botVoiceData = (BotVoiceData)voiceDataList.FirstOrDefault(vd => vd is BotVoiceData peerVoiceData && peerVoiceData.Agent.Index == agent.Index);
                botVoiceData.UpdateVoiceData(dataBuffer, bufferSize);
            }
        }

        public void PlayAll()
        {
            Vec3 listenerPosition = Listener.ControlledAgent?.Position ?? Mission.Current.GetCameraFrame().origin;
            Mat3 listenerRotation = Listener.ControlledAgent?.LookRotation ?? Mission.Current.GetCameraFrame().rotation;
            foreach (VoiceData vd in voiceDataList)
            {
                vd.UpdatePanning(listenerPosition, listenerRotation);
                vd.Play();
            }
        }

        public bool IsFull()
        {
            return voiceDataList.Count > VoipConstants.MAX_CONCURRENT_VOICES;
        }

        public void CleanupExpiredVoices()
        {
            TimeSpan expiredTimeSpan = TimeSpan.FromSeconds(2);
            List<VoiceData> expiredVoiceData = voiceDataList.Where(vd => vd.IsExpired(expiredTimeSpan)).ToList();
            foreach (VoiceData expiredVD in expiredVoiceData)
            {
                expiredVD.Dispose();
                voiceDataList.Remove(expiredVD);
            }
        }
    }
}
