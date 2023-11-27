using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CS_VehicleSyncHonk : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public CS_VehicleSyncHonk(MissionObjectId missionObjectId)
        {
            MissionObjectId = missionObjectId;
        }

        public CS_VehicleSyncHonk()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting vehicle with id: {MissionObjectId.Id} to honk";
        }
    }
}