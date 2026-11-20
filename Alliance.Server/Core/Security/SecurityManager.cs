using Alliance.Client.Extensions.AdminMenu;
using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.Models;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Security
{
	/// <summary>
	/// Server-side class used to update players roles and access level.
	/// </summary>
	public static class SecurityManager
	{
		private static AL_PlayerData GetOrCreate(PlayerId id, NetworkCommunicator peer = null)
		{
			if (id == PlayerId.Empty) return null;
			if (!PlayerStore.Instance.AllPlayersData.TryGetValue(id, out AL_PlayerData data))
			{
				data = new AL_PlayerData(peer?.UserName, id);
				PlayerStore.Instance.AllPlayersData[id] = data;
			}
			return data;
		}

		private static void Kick(PlayerId id)
		{
			DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(id, false);
		}

		public static void WarnPlayer(PlayerId playerId, NetworkCommunicator player = null, string reason = "Warning issued")
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.WarningCount++;
			data.LastWarning = reason;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			CommonAdminMsg.SendNotificationToPeerAsServer(player, reason);
			Log($"[Security] {data.Name} warned: {reason}");
		}

		public static void KickPlayer(PlayerId playerId, NetworkCommunicator player = null, string reason = "Kicked by admin")
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.KickCount++;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Kick(playerId);
			Log($"[Security] {data.Name} kicked. Reason: {reason}");
		}

		public static void BanPlayer(PlayerId playerId, NetworkCommunicator player = null, string reason = "Banned", DateTime? until = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.IsBanned = true;
			data.BanCount++;
			data.LastBanReason = reason;
			data.SanctionEnd = until ?? DateTime.MaxValue;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Kick(playerId);
			Log($"[Security] {data.Name} banned until {data.SanctionEnd} ({reason})");
		}

		public static void UnbanPlayer(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.IsBanned = false;
			data.SanctionEnd = DateTime.Now;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} unbanned.");
		}

		public static void MutePlayer(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.IsMuted = true;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} muted.");
		}

		public static void UnmutePlayer(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.IsMuted = false;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} unmuted.");
		}

		public static void GrantAdmin(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.Admin = true;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} promoted to admin.");
		}

		public static void RevokeAdmin(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.Admin = false;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} removed from admin list.");
		}

		public static void GrantSudo(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.Sudo = true;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} granted SUDO access.");
		}

		public static void RevokeSudo(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.Sudo = false;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} SUDO access revoked.");
		}

		public static void GrantVIP(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null) return;

			data.VIP = true;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} granted VIP status.");
		}

		public static void RevokeVIP(PlayerId playerId, NetworkCommunicator player = null)
		{
			AL_PlayerData data = GetOrCreate(playerId, player);
			if (data == null || !data.VIP) return;

			data.VIP = false;

			PlayerService.ApplyPlayerDataUpdate(data, player);
			Log($"[Security] {data.Name} VIP status removed.");
		}
	}
}