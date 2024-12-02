using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncSetActionChannel : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public int AgentIndex { get; private set; }

        public int Action { get; private set; }

        public SyncSetActionChannel(MissionObjectId missionObjectId, int agentIndex, int act)
        {
            MissionObjectId = missionObjectId;
            AgentIndex = agentIndex;
            Action = act;
        }

        public SyncSetActionChannel()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            AgentIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
            Action = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteAgentIndexToPacket(AgentIndex);
            WriteIntToPacket(Action, new CompressionInfo.Integer(0, 1000, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize action : ", Action, " of UserAgent ", AgentIndex, " with Id: ", MissionObjectId.Id);
        }
    }
}