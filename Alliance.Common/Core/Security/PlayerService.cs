using Alliance.Common.Core.Security.Models;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Security
{
	public class PlayerService
	{
		public static event Action<AL_PlayerData, NetworkCommunicator> PlayerDataUpdated;

		/// <summary>
		/// Apply a change to a player's data (creates entry if needed),
		/// then saves and syncs it to clients.
		/// </summary>
		public static void ApplyPlayerDataUpdate(AL_PlayerData updatedData, NetworkCommunicator player = null, bool allData = false)
		{
			if (updatedData == null || updatedData.Id == PlayerId.Empty)
				return;

			// Ensure the record exists
			if (!PlayerStore.Instance.AllPlayersData.TryGetValue(updatedData.Id, out AL_PlayerData playerData))
			{
				playerData = new AL_PlayerData(updatedData.Name, updatedData.Id);
				PlayerStore.Instance.AllPlayersData[updatedData.Id] = playerData;
			}

			// Apply changes (partial or full)
			MergeData(playerData, updatedData, allData);

			// Update runtime caches
			RefreshRuntimeLists(updatedData.Id, player);

			if (GameNetwork.IsServer)
			{
				// Save and broadcast to clients
				PlayerStore.Instance.Save();
				PlayerSyncService.BroadcastPlayerData(playerData, player);
			}

			PlayerDataUpdated?.Invoke(playerData, player);
		}

		/// <summary>
		/// Merge the incoming data into the stored one.
		/// If allData = false, only basic fields are updated.
		/// </summary>
		private static void MergeData(AL_PlayerData target, AL_PlayerData source, bool allData)
		{
			target.Name = source.Name ?? target.Name;
			target.Sudo = source.Sudo;
			target.Admin = source.Admin;
			target.IsMuted = source.IsMuted;
			target.VIP = source.VIP;

			if (allData)
			{
				target.WarningCount = source.WarningCount;
				target.LastWarning = source.LastWarning;
				target.KickCount = source.KickCount;
				target.BanCount = source.BanCount;
				target.IsBanned = source.IsBanned;
				target.LastBanReason = source.LastBanReason;
				target.SanctionEnd = source.SanctionEnd;
			}
		}

		/// <summary>
		/// Update runtime Online lists based on this player’s current state.
		/// </summary>
		private static void RefreshRuntimeLists(PlayerId id, NetworkCommunicator player)
		{
			// If player is connected, refresh online caches
			if (player != null)
			{
				AL_PlayerData data = PlayerStore.Instance.AllPlayersData[id];
				PlayerStore.Instance.OnlinePlayersData[player] = data;
				PlayerStore.Instance.PlayerIdToCommunicator[id] = player;

				if (data.IsAdmin)
					PlayerStore.Instance.OnlineAdmins.Add(player);
				else
					PlayerStore.Instance.OnlineAdmins.Remove(player);
			}
		}
	}
}
