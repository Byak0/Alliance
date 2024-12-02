using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncAbilityOfNavmesh : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public bool Navmesh1 { get; private set; }

        public bool Navmesh2 { get; private set; }

        public SyncAbilityOfNavmesh(MissionObjectId missionObjectId, bool navmesh1, bool navmesh2)
        {
            MissionObjectId = missionObjectId;
            Navmesh1 = navmesh1;
            Navmesh2 = navmesh2;
        }

        public SyncAbilityOfNavmesh()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            Navmesh1 = ReadBoolFromPacket(ref bufferReadValid);
            Navmesh2 = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteBoolToPacket(Navmesh1);
            WriteBoolToPacket(Navmesh2);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize Navmesh1: ", Navmesh1, " | Navmesh2 : ", Navmesh2, " of MissionObject with Id: ", MissionObjectId.Id);
        }
    }
}