﻿using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CS_VehicleSyncTurnRight : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }
        public bool Turn { get; private set; }

        public CS_VehicleSyncTurnRight(MissionObjectId missionObjectId, bool turn)
        {
            MissionObjectId = missionObjectId;
            Turn = turn;
        }

        public CS_VehicleSyncTurnRight()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            Turn = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteBoolToPacket(Turn);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return $"Requesting vehicle with id: {MissionObjectId.Id} to turn right ({Turn})";
        }
    }
}