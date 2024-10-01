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
		public bool HealAll { get; set; }
		public bool GodMod { get; set; }
		public bool GodModAll { get; set; }
		public bool Kill { get; set; }
		public bool KillAll { get; set; }
		public bool Kick { get; set; }
		public bool Ban { get; set; }
		public bool Mute { get; set; }
		public bool Respawn { get; set; }
		public bool SetAdmin { get; set; }
		public bool ToggleInvulnerable { get; set; }
		public bool TeleportToPlayer { get; set; }
		public bool TeleportPlayerToYou { get; set; }
		public bool TeleportAllPlayerToYou { get; set; }
		public bool SendWarningToPlayer { get; set; }

		protected override void OnWrite()
		{
			WriteBoolToPacket(Heal);
			WriteBoolToPacket(HealAll);
			WriteBoolToPacket(GodMod);
			WriteBoolToPacket(GodModAll);
			WriteBoolToPacket(Kill);
			WriteBoolToPacket(KillAll);
			WriteBoolToPacket(Kick);
			WriteBoolToPacket(Ban);
            WriteBoolToPacket(Mute);
            WriteBoolToPacket(Respawn);
			WriteBoolToPacket(SetAdmin);
			WriteBoolToPacket(ToggleInvulnerable);
			WriteBoolToPacket(TeleportToPlayer);
			WriteBoolToPacket(TeleportPlayerToYou);
			WriteBoolToPacket(TeleportAllPlayerToYou);
			WriteBoolToPacket(SendWarningToPlayer);
			WriteStringToPacket(PlayerSelected);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Heal = ReadBoolFromPacket(ref bufferReadValid);
			HealAll = ReadBoolFromPacket(ref bufferReadValid);
			GodMod = ReadBoolFromPacket(ref bufferReadValid);
			GodModAll = ReadBoolFromPacket(ref bufferReadValid);
			Kill = ReadBoolFromPacket(ref bufferReadValid);
			KillAll = ReadBoolFromPacket(ref bufferReadValid);
			Kick = ReadBoolFromPacket(ref bufferReadValid);
			Ban = ReadBoolFromPacket(ref bufferReadValid);
            Mute = ReadBoolFromPacket(ref bufferReadValid);
            Respawn = ReadBoolFromPacket(ref bufferReadValid);
			SetAdmin = ReadBoolFromPacket(ref bufferReadValid);
			ToggleInvulnerable = ReadBoolFromPacket(ref bufferReadValid);
			TeleportToPlayer = ReadBoolFromPacket(ref bufferReadValid);
			TeleportPlayerToYou = ReadBoolFromPacket(ref bufferReadValid);
			TeleportAllPlayerToYou = ReadBoolFromPacket(ref bufferReadValid);
			SendWarningToPlayer = ReadBoolFromPacket(ref bufferReadValid);
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
