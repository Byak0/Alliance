using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CS_VehicleSyncLight : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }
        public bool LightOn { get; private set; }

        public CS_VehicleSyncLight(MissionObjectId missionObjectId, bool lightOn)
        {
            MissionObjectId = missionObjectId;
            LightOn = lightOn;
        }

        public CS_VehicleSyncLight()
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