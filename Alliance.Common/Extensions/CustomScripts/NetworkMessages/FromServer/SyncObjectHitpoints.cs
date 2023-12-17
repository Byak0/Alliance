using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncObjectHitpoints : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public float Hitpoints { get; private set; }

        public SyncObjectHitpoints(MissionObjectId missionObjectId, float hitpoints)
        {
            MissionObjectId = missionObjectId;
            Hitpoints = hitpoints;
        }

        public SyncObjectHitpoints()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            Hitpoints = ReadFloatFromPacket(CompressionMission.UsableGameObjectHealthCompressionInfo, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteFloatToPacket(MathF.Max(Hitpoints, 0f), CompressionMission.UsableGameObjectHealthCompressionInfo);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize HitPoints: ", Hitpoints, " of MissionObject with Id: ", MissionObjectId.Id);
        }
    }
}