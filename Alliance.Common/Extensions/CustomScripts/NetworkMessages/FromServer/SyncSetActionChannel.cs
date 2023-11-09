using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncSetActionChannel : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }

        public Agent UserAgent { get; private set; }

        public int Action { get; private set; }

        public SyncSetActionChannel(MissionObject missionObject, Agent userAgent, int act)
        {
            MissionObject = missionObject;
            UserAgent = userAgent;
            Action = act;
        }

        public SyncSetActionChannel()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            UserAgent = ReadAgentReferenceFromPacket(ref bufferReadValid);
            Action = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
            WriteAgentReferenceToPacket(UserAgent);
            WriteIntToPacket(Action, new CompressionInfo.Integer(0, 1000, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize action : ", Action, " of UserAgent ", UserAgent.Name, " with Id: ", MissionObject.Id, " and name: ", MissionObject.GameEntity.Name);
        }
    }
}