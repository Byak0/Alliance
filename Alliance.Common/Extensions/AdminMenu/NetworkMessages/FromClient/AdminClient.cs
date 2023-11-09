using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class AdminClient : GameNetworkMessage
    {
        public AdminClient()
        { }

        public string PlayerSelected { get; set; }
        public bool Heal { get; set; }
        public bool GodMod { get; set; }
        public bool Kill { get; set; }
        public bool Kick { get; set; }
        public bool Ban { get; set; }
        public bool SetAdmin { get; set; }
        public bool ToggleInvulnerable { get; set; }
        public bool TeleportToPlayer { get; set; }
        public bool TeleportPlayerToYou { get; set; }

        protected override void OnWrite()
        {
            WriteBoolToPacket(Heal);
            WriteBoolToPacket(GodMod);
            WriteBoolToPacket(Kill);
            WriteBoolToPacket(Kick);
            WriteBoolToPacket(Ban);
            WriteBoolToPacket(SetAdmin);
            WriteBoolToPacket(ToggleInvulnerable);
            WriteBoolToPacket(TeleportToPlayer);
            WriteBoolToPacket(TeleportPlayerToYou);
            WriteStringToPacket(PlayerSelected);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Heal = ReadBoolFromPacket(ref bufferReadValid);
            GodMod = ReadBoolFromPacket(ref bufferReadValid);
            Kill = ReadBoolFromPacket(ref bufferReadValid);
            Kick = ReadBoolFromPacket(ref bufferReadValid);
            Ban = ReadBoolFromPacket(ref bufferReadValid);
            SetAdmin = ReadBoolFromPacket(ref bufferReadValid);
            ToggleInvulnerable = ReadBoolFromPacket(ref bufferReadValid);
            TeleportToPlayer = ReadBoolFromPacket(ref bufferReadValid);
            TeleportPlayerToYou = ReadBoolFromPacket(ref bufferReadValid);
            PlayerSelected = ReadStringFromPacket(ref bufferReadValid);

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }

        protected override string OnGetLogFormat()
        {
            return "AdminNetworkMessage";
        }
    }
}
