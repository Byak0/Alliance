using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CS_VehicleSyncSpeed : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }
        public float Speed { get; private set; }

        public CS_VehicleSyncSpeed(MissionObjectId missionObjectId, float speed)
        {
            MissionObjectId = missionObjectId;
            Speed = speed;
        }

        public CS_VehicleSyncSpeed()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            Speed = ReadFloatFromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteFloatToPacket(Speed, CompressionBasic.PositionCompressionInfo);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Syncing vehicle speed {Speed} for id: {MissionObjectId.Id}";
        }
    }
}
