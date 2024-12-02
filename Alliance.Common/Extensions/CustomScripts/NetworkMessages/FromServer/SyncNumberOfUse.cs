using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncNumberOfUse : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public int NumberOfUse { get; private set; }

        public SyncNumberOfUse(MissionObjectId missionObjectId, int numberOfUse)
        {
            MissionObjectId = missionObjectId;
            NumberOfUse = numberOfUse;
        }

        public SyncNumberOfUse()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            NumberOfUse = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteIntToPacket(NumberOfUse, new CompressionInfo.Integer(0, 1000, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize NumberOfUse : ", NumberOfUse, " with Id: ", MissionObjectId.Id);
        }
    }
}