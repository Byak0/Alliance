using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class CS_VehicleRequestTurnRight : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }
        public bool Turn { get; private set; }

        public CS_VehicleRequestTurnRight(MissionObject missionObject, bool turn)
        {
            MissionObject = missionObject;
            Turn = turn;
        }

        public CS_VehicleRequestTurnRight()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            Turn = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
            WriteBoolToPacket(Turn);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting Entity with id: {MissionObject.Id} and name: {MissionObject.GameEntity.Name} to turn right ({Turn})";
        }
    }
}