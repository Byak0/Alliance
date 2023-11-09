using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class CS_VehicleRequestHonk : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }

        public CS_VehicleRequestHonk(MissionObject missionObject)
        {
            MissionObject = missionObject;
        }

        public CS_VehicleRequestHonk()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting Entity with id: {MissionObject.Id} and name: {MissionObject.GameEntity.Name} to honk";
        }
    }
}