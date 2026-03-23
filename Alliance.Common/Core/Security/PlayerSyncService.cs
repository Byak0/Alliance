using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Security.Models;
using Alliance.Common.Core.Security.NetworkMessages.FromServer;
using Alliance.Common.Core.Utils;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Security
{
	public class PlayerSyncService
	{
		#region Server messages
		public static void SendPlayerStoreToClient(NetworkCommunicator client)
		{
			if (client.IsAdmin())
			{
				// Send all players data to admins
				foreach (AL_PlayerData playerData in PlayerStore.Instance.AllPlayersData.Values)
				{
					PlayerStore.Instance.PlayerIdToCommunicator.TryGetValue(playerData.Id, out NetworkCommunicator networkCommunicator);
					GameNetwork.BeginModuleEventAsServer(client);
					GameNetwork.WriteMessage(new SyncPlayerData(playerData, networkCommunicator, allData: true));
					GameNetwork.EndModuleEventAsServer();
				}
			}
			else
			{
				// Send only online players data to regular players
				foreach (KeyValuePair<NetworkCommunicator, AL_PlayerData> kvp in PlayerStore.Instance.OnlinePlayersData)
				{
					GameNetwork.BeginModuleEventAsServer(client);
					GameNetwork.WriteMessage(new SyncPlayerData(kvp.Value, kvp.Key, allData: false));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}

		public static void BroadcastPlayerStore()
		{
			foreach (NetworkCommunicator client in GameNetwork.NetworkPeers)
			{
				if (client.IsAdmin())
				{
					// Send all players data to admins
					foreach (AL_PlayerData playerData in PlayerStore.Instance.AllPlayersData.Values)
					{
						PlayerStore.Instance.PlayerIdToCommunicator.TryGetValue(playerData.Id, out NetworkCommunicator networkCommunicator);
						GameNetwork.BeginModuleEventAsServer(client);
						GameNetwork.WriteMessage(new SyncPlayerData(playerData, networkCommunicator, allData: true));
						GameNetwork.EndModuleEventAsServer();
					}
				}
				else
				{
					// Send only online players data to regular players
					foreach (KeyValuePair<NetworkCommunicator, AL_PlayerData> kvp in PlayerStore.Instance.OnlinePlayersData)
					{
						GameNetwork.BeginModuleEventAsServer(client);
						GameNetwork.WriteMessage(new SyncPlayerData(kvp.Value, kvp.Key, allData: false));
						GameNetwork.EndModuleEventAsServer();
					}
				}
			}
		}

		public static void BroadcastPlayerData(AL_PlayerData playerData, NetworkCommunicator player = null)
		{
			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(new SyncPlayerData(playerData, player, peer.IsAdmin()));
				GameNetwork.EndModuleEventAsServer();
			}
		}
		#endregion

		#region Helpers for packet reading/writing
		public static (NetworkCommunicator, AL_PlayerData, bool) ReadPlayerDataFromPacket(ref bool bufferReadValid)
		{
			bool allDataAvailable = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);

			AL_PlayerData playerData = new();
			bool peerReferenceAvailable = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			NetworkCommunicator player = null;
			PlayerId playerId = PlayerId.Empty;
			if (peerReferenceAvailable)
			{
				player = GameNetworkMessage.ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
				playerData.Name = player?.UserName;
			}
			else
			{
				playerId = GameNetworkExtensions.ReadPlayerIdFromPacket(ref bufferReadValid);
				playerData.Name = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
			}
			playerData.Id = player?.VirtualPlayer.Id ?? playerId;
			playerData.Sudo = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			playerData.Admin = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			playerData.IsMuted = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			playerData.VIP = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);

			if (allDataAvailable)
			{
				playerData.WarningCount = GameNetworkMessage.ReadIntFromPacket(CompressionHelper.IntValueCompressionInfoMax255, ref bufferReadValid);
				playerData.LastWarning = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
				playerData.KickCount = GameNetworkMessage.ReadIntFromPacket(CompressionHelper.IntValueCompressionInfoMax255, ref bufferReadValid);
				playerData.BanCount = GameNetworkMessage.ReadIntFromPacket(CompressionHelper.IntValueCompressionInfoMax255, ref bufferReadValid);
				playerData.IsBanned = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
				playerData.LastBanReason = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
				playerData.SanctionEnd = GameNetworkExtensions.ReadDateTimeFromPacket(ref bufferReadValid);
			}
			return (player, playerData, allDataAvailable);
		}

		public static void WritePlayerDataToPacket(AL_PlayerData playerData, NetworkCommunicator player = null, bool allData = false)
		{
			GameNetworkMessage.WriteBoolToPacket(allData);

			if (player != null)
			{
				// Use player reference if available (lighter packet but player must be connected)
				GameNetworkMessage.WriteBoolToPacket(true);
				GameNetworkMessage.WriteNetworkPeerReferenceToPacket(player);
			}
			else
			{
				// Else use player ID
				GameNetworkMessage.WriteBoolToPacket(false);
				GameNetworkExtensions.WritePlayerIdToPacket(playerData.Id);
				if (allData) GameNetworkMessage.WriteStringToPacket(playerData.Name);
			}
			GameNetworkMessage.WriteBoolToPacket(playerData.Sudo);
			GameNetworkMessage.WriteBoolToPacket(playerData.Admin);
			GameNetworkMessage.WriteBoolToPacket(playerData.IsMuted);
			GameNetworkMessage.WriteBoolToPacket(playerData.VIP);

			if (allData)
			{
				GameNetworkMessage.WriteIntToPacket(playerData.WarningCount, CompressionHelper.IntValueCompressionInfoMax255);
				GameNetworkMessage.WriteStringToPacket(playerData.LastWarning);
				GameNetworkMessage.WriteIntToPacket(playerData.KickCount, CompressionHelper.IntValueCompressionInfoMax255);
				GameNetworkMessage.WriteIntToPacket(playerData.BanCount, CompressionHelper.IntValueCompressionInfoMax255);
				GameNetworkMessage.WriteBoolToPacket(playerData.IsBanned);
				GameNetworkMessage.WriteStringToPacket(playerData.LastBanReason);
				GameNetworkExtensions.WriteDateTimeToPacket(playerData.SanctionEnd);
			}
		}
		#endregion
	}
}