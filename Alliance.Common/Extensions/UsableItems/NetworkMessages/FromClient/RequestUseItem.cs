using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.UsableItems.NetworkMessages.FromClient
{
    /// <summary>
    /// Request to play equipped item.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestUseItem : GameNetworkMessage
    {
        static readonly CompressionInfo.Integer EquipmentCompressionInfo = new CompressionInfo.Integer((int)EquipmentIndex.WeaponItemBeginSlot, (int)EquipmentIndex.NumEquipmentSetSlots, true);

        public EquipmentIndex EquipmentIndex { get; private set; }

        public RequestUseItem() { }

        public RequestUseItem(EquipmentIndex equipmentIndex)
        {
            EquipmentIndex = equipmentIndex;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket((int)EquipmentIndex, EquipmentCompressionInfo);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            EquipmentIndex = (EquipmentIndex)ReadIntFromPacket(EquipmentCompressionInfo, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Equipment;
        }

        protected override string OnGetLogFormat()
        {
            return "Request usage of equipped item.";
        }
    }
}
