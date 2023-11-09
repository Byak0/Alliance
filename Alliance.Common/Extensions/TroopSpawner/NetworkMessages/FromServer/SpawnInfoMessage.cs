using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SpawnInfoMessage : GameNetworkMessage
    {
        private BasicCharacterObject troop;
        private int troopCount;
        private int troopLeft;

        public SpawnInfoMessage() { }

        public SpawnInfoMessage(BasicCharacterObject troop, int troopCount, int troopLeft)
        {
            this.troop = troop;
            this.troopCount = troopCount;
            this.troopLeft = troopLeft;
        }

        public BasicCharacterObject Troop
        {
            get
            {
                return troop;
            }
            private set
            {
                troop = value;
            }
        }

        public int TroopCount
        {
            get
            {
                return troopCount;
            }
            private set
            {
                troopCount = value;
            }
        }

        public int TroopLeft
        {
            get
            {
                return troopLeft;
            }
            private set
            {
                troopLeft = value;
            }
        }

        protected override void OnWrite()
        {
            WriteObjectReferenceToPacket(troop, CompressionBasic.GUIDCompressionInfo);
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
