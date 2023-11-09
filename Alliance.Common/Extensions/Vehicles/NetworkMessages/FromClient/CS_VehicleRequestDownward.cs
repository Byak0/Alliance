using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class CS_VehicleRequestDownward : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }
        public bool Move { get; private set; }

        public CS_VehicleRequestDownward(MissionObject missionObject, bool move)
        {
            MissionObject = missionObject;
            Move = move;
        }

        public CS_VehicleRequestDownward()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            Move = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
            WriteBoolToPacket(Move);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting Entity with id: {MissionObject.Id} and name: {MissionObject.GameEntity.Name} to move downward ({Move})";
        }
    }
}