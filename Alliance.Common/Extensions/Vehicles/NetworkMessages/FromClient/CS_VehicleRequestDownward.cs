using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class CS_VehicleRequestDownward : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }
        public bool Move { get; private set; }

        public CS_VehicleRequestDownward(MissionObjectId missionObjectId, bool move)
        {
            MissionObjectId = missionObjectId;
            Move = move;
        }

        public CS_VehicleRequestDownward()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            Move = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteBoolToPacket(Move);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting vehicle with id: {MissionObjectId.Id} to move downward ({Move})";
        }
    }
}