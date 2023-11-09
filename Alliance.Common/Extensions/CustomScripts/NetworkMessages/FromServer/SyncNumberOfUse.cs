using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncNumberOfUse : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }

        public int NumberOfUse { get; private set; }

        public SyncNumberOfUse(MissionObject missionObject, int numberOfUse)
        {
            MissionObject = missionObject;
            NumberOfUse = numberOfUse;
        }

        public SyncNumberOfUse()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            NumberOfUse = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
            WriteIntToPacket(NumberOfUse, new CompressionInfo.Integer(0, 1000, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize NumberOfUse : ", NumberOfUse, " with Id: ", MissionObject.Id, " and name: ", MissionObject.GameEntity.Name);
        }
    }
}