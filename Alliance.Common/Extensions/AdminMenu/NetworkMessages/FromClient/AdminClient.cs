using Alliance.Common.Core.Utils;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class AdminClient : GameNetworkMessage
	{
		public AdminClient()
		{ }

		// todo refactor to use an enum/flags instead of a million bools ?
		public PlayerId PlayerSelected { get; set; }
		public bool Heal { get; set; }
		public bool HealAll { get; set; }
		public bool GodMod { get; set; }
		public bool GodModAll { get; set; }
		public bool Kill { get; set; }
		public bool KillPlayers { get; set; }
		public bool KillBots { get; set; }
		public bool Kick { get; set; }
		public bool Ban { get; set; }
		public bool Unban { get; set; }
		public bool ToggleMutePlayer { get; set; }
		public bool Respawn { get; set; }
		public bool SetSudo { get; set; }
		public bool SetAdmin { get; set; }
		public bool SetVIP { get; set; }
		public bool ToggleInvulnerable { get; set; }
		public bool TeleportToPlayer { get; set; }
		public bool TeleportPlayerToYou { get; set; }
		public bool TeleportAllPlayerToYou { get; set; }
		public bool SendWarningToPlayer { get; set; }
		public string WarningMessageToPlayer { get; set; }
		public string BanReason { get; set; }

		protected override void OnWrite()
		{
			GameNetworkExtensions.WritePlayerIdToPacket(PlayerSelected);
			WriteBoolToPacket(Heal);
			WriteBoolToPacket(HealAll);
			WriteBoolToPacket(GodMod);
			WriteBoolToPacket(GodModAll);
			WriteBoolToPacket(Kill);
			WriteBoolToPacket(KillPlayers);
			WriteBoolToPacket(KillBots);
			WriteBoolToPacket(Kick);
			WriteBoolToPacket(Ban);
			WriteBoolToPacket(Unban);
			WriteBoolToPacket(ToggleMutePlayer);
			WriteBoolToPacket(Respawn);
			WriteBoolToPacket(SetSudo);
			WriteBoolToPacket(SetAdmin);
			WriteBoolToPacket(SetVIP);
			WriteBoolToPacket(ToggleInvulnerable);
			WriteBoolToPacket(TeleportToPlayer);
			WriteBoolToPacket(TeleportPlayerToYou);
			WriteBoolToPacket(TeleportAllPlayerToYou);
			WriteBoolToPacket(SendWarningToPlayer);
			WriteStringToPacket(WarningMessageToPlayer);
			WriteStringToPacket(BanReason ?? "");
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			PlayerSelected = GameNetworkExtensions.ReadPlayerIdFromPacket(ref bufferReadValid);
			Heal = ReadBoolFromPacket(ref bufferReadValid);
			HealAll = ReadBoolFromPacket(ref bufferReadValid);
			GodMod = ReadBoolFromPacket(ref bufferReadValid);
			GodModAll = ReadBoolFromPacket(ref bufferReadValid);
			Kill = ReadBoolFromPacket(ref bufferReadValid);
			KillPlayers = ReadBoolFromPacket(ref bufferReadValid);
			KillBots = ReadBoolFromPacket(ref bufferReadValid);
			Kick = ReadBoolFromPacket(ref bufferReadValid);
			Ban = ReadBoolFromPacket(ref bufferReadValid);
			Unban = ReadBoolFromPacket(ref bufferReadValid);
			ToggleMutePlayer = ReadBoolFromPacket(ref bufferReadValid);
			Respawn = ReadBoolFromPacket(ref bufferReadValid);
			SetSudo = ReadBoolFromPacket(ref bufferReadValid);
			SetAdmin = ReadBoolFromPacket(ref bufferReadValid);
			SetVIP = ReadBoolFromPacket(ref bufferReadValid);
			ToggleInvulnerable = ReadBoolFromPacket(ref bufferReadValid);
			TeleportToPlayer = ReadBoolFromPacket(ref bufferReadValid);
			TeleportPlayerToYou = ReadBoolFromPacket(ref bufferReadValid);
			TeleportAllPlayerToYou = ReadBoolFromPacket(ref bufferReadValid);
			SendWarningToPlayer = ReadBoolFromPacket(ref bufferReadValid);
			WarningMessageToPlayer = ReadStringFromPacket(ref bufferReadValid);
			BanReason = ReadStringFromPacket(ref bufferReadValid);

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
