using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncSoundObject : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public int AgentIndex { get; private set; }

        public SyncSoundObject(MissionObjectId missionObjectId, int agentIndex)
        {
            MissionObjectId = missionObjectId;
            AgentIndex = agentIndex;
        }

        public SyncSoundObject()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            AgentIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteAgentIndexToPacket(AgentIndex);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize sound of UserAgent ", AgentIndex, " with Id: ", MissionObjectId.Id);
        }
    }
}