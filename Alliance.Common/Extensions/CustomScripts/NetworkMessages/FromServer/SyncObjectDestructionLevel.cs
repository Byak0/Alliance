using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncObjectDestructionLevel : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public int DestructionLevel { get; private set; }

        public int ForcedIndex { get; private set; }

        public float BlowMagnitude { get; private set; }

        public Vec3 BlowPosition { get; private set; }

        public Vec3 BlowDirection { get; private set; }

        public SyncObjectDestructionLevel(MissionObjectId missionObjectId, int destructionLevel, int forcedIndex, float blowMagnitude, Vec3 blowPosition, Vec3 blowDirection)
        {
            MissionObjectId = missionObjectId;
            DestructionLevel = destructionLevel;
            ForcedIndex = forcedIndex;
            BlowMagnitude = blowMagnitude;
            BlowPosition = blowPosition;
            BlowDirection = blowDirection;
        }

        public SyncObjectDestructionLevel()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            DestructionLevel = ReadIntFromPacket(CompressionMission.UsableGameObjectDestructionStateCompressionInfo, ref bufferReadValid);
            ForcedIndex = ReadBoolFromPacket(ref bufferReadValid) ? ReadIntFromPacket(CompressionBasic.MissionObjectIDCompressionInfo, ref bufferReadValid) : -1;
            BlowMagnitude = ReadFloatFromPacket(CompressionMission.UsableGameObjectBlowMagnitude, ref bufferReadValid);
            BlowPosition = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
            BlowDirection = ReadVec3FromPacket(CompressionMission.UsableGameObjectBlowDirection, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteIntToPacket(DestructionLevel, CompressionMission.UsableGameObjectDestructionStateCompressionInfo);
            WriteBoolToPacket(ForcedIndex != -1);
            if (ForcedIndex != -1)
            {
                WriteIntToPacket(ForcedIndex, CompressionBasic.MissionObjectIDCompressionInfo);
            }

            WriteFloatToPacket(BlowMagnitude, CompressionMission.UsableGameObjectBlowMagnitude);
            WriteVec3ToPacket(BlowPosition, CompressionBasic.PositionCompressionInfo);
            WriteVec3ToPacket(BlowDirection, CompressionMission.UsableGameObjectBlowDirection);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize DestructionLevel: ", DestructionLevel, " of MissionObject with Id: ", MissionObjectId.Id, ForcedIndex != -1 ? " (New object will have ID: " + ForcedIndex + ")" : "");
        }
    }
}