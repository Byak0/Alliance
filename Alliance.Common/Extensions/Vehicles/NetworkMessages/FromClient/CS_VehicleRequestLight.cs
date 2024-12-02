using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class CS_VehicleRequestLight : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }
        public bool LightOn { get; private set; }

        public CS_VehicleRequestLight(MissionObjectId missionObjectId, bool lightOn)
        {
            MissionObjectId = missionObjectId;
            LightOn = lightOn;
        }

        public CS_VehicleRequestLight()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            LightOn = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteBoolToPacket(LightOn);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting vehicle with id: {MissionObjectId.Id} to turn the lights ({LightOn})";
        }
    }
}