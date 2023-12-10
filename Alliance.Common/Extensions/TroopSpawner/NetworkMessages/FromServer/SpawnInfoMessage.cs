using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SpawnInfoMessage : GameNetworkMessage
    {
        public BasicCharacterObject Troop { get; private set; }
        public int TroopCount { get; private set; }
        public int TroopLeft { get; private set; }

        public SpawnInfoMessage() { }

        public SpawnInfoMessage(BasicCharacterObject troop, int troopCount, int troopLeft)
        {
            Troop = troop;
            TroopCount = troopCount;
            TroopLeft = troopLeft;
        }

        protected override void OnWrite()
        {
            WriteObjectReferenceToPacket(Troop, CompressionBasic.GUIDCompressionInfo);
            WriteIntToPacket(TroopCount, new CompressionInfo.Integer(0, 9999, true));
            WriteIntToPacket(TroopLeft, new CompressionInfo.Integer(0, 9999, true));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Troop = (BasicCharacterObject)ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
            TroopCount = ReadIntFromPacket(new CompressionInfo.Integer(0, 9999, true), ref bufferReadValid);
            TroopLeft = ReadIntFromPacket(new CompressionInfo.Integer(0, 9999, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }

        protected override string OnGetLogFormat()
        {
            return "Update number of troops spawned";
        }
    }
}
