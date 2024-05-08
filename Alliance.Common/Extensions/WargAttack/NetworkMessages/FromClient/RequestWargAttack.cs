using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.WargAttack.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestWargAttack : GameNetworkMessage
    {
        static readonly CompressionInfo.Integer EquipmentCompressionInfo = new CompressionInfo.Integer((int)EquipmentIndex.WeaponItemBeginSlot, (int)EquipmentIndex.NumEquipmentSetSlots, true);

        //public EquipmentIndex EquipmentIndex { get; private set; }

        public RequestWargAttack() { }

        //public RequestUseItem(EquipmentIndex equipmentIndex)
        //{
        //    EquipmentIndex = equipmentIndex;
        //}

        protected override void OnWrite()
        {
            //WriteIntToPacket((int)EquipmentIndex, EquipmentCompressionInfo);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            //EquipmentIndex = (EquipmentIndex)ReadIntFromPacket(EquipmentCompressionInfo, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return "Request usage of WarkAttack.";
        }
    }
}
