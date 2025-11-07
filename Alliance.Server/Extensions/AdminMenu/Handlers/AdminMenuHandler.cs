using Alliance.Client.Extensions.AdminMenu;
using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Security.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.RTSCamera.Extension;
using Alliance.Server.Core;
using Alliance.Server.Core.Security;
using Alliance.Server.Extensions.AdminMenu.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.Extensions.AdminMenu.Handlers
{
	public class AdminMenuHandler : IHandlerRegister
	{
		private bool _invulnerable;

		public AdminMenuHandler()
		{
		}

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<AdminClient>(InitAdminServer);
			reg.Register<RequestNotification>(HandleNotificationRequest);
			reg.Register<SpawnHorseRequest>(HandleSpawnHorseRequest);
			reg.Register<TeleportRequest>(HandleTeleportRequest);
		}

		public bool HandleSpawnHorseRequest(NetworkCommunicator peer, SpawnHorseRequest req)
		{
			if (peer.IsAdmin())
			{
				string horseId = "mp_empire_horse_agile";
				string reinsId = "mp_imperial_riding_harness";

				ItemObject horseItem = Game.Current.ObjectManager.GetObject<ItemObject>(horseId);
				ItemObject reinsItem = Game.Current.ObjectManager.GetObject<ItemObject>(reinsId);

				// Ensure the ItemObject is a horse
				if (horseItem.IsMountable)
				{
					EquipmentElement horseEquipmentElement = new EquipmentElement(horseItem);
					EquipmentElement harnessEquipmentElement = new EquipmentElement(reinsItem);

					// Spawn the horse agent
					Agent horseAgent = Mission.Current.SpawnMonster(horseEquipmentElement, harnessEquipmentElement, new Vec3(10f, 10f, 1f), new Vec2(1, 0));

					// Make the horse move to the player
					WorldPosition target = peer.ControlledAgent.GetWorldPosition();
					horseAgent.SetScriptedPositionAndDirection(ref target, 1f, false, AIScriptedFrameFlags.None);

					return true;
				}
			}

			return false;
		}

		public bool HandleTeleportRequest(NetworkCommunicator peer, TeleportRequest req)
		{
			if (!peer.IsAdmin() || peer.ControlledAgent == null) return false;

			peer.ControlledAgent.TeleportToPosition(req.Position);
			Log($"[AdminPanel][TP] Admin {peer.UserName} teleported to {req.Position}", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[TP] Admin {peer.UserName} teleported to {req.Position}", AdminServerLog.ColorList.Success);

			return true;
		}

		public bool HandleNotificationRequest(NetworkCommunicator peer, RequestNotification notification)
		{
			if (peer.IsAdmin())
			{
				CommonAdminMsg.SendNotificationToAll(notification.Text, notification.NotificationType);
			}

			return false;
		}

		public bool InitAdminServer(NetworkCommunicator peer, AdminClient admin)
		{
			if (peer.IsAdmin())
			{
				if (admin.Heal)
					return HealPlayer(peer, admin);
				if (admin.HealAll)
					return HealAll(peer);
				if (admin.GodMod)
					return GodMod(peer, admin);
				if (admin.GodModAll)
					return GodModAll(peer);
				if (admin.Kill)
					return Kill(peer, admin);
				if (admin.KillPlayers)
					return KillPlayers(peer);
				if (admin.KillBots)
					return KillBots(peer);
				if (admin.Kick)
					return Kick(peer, admin);
				if (admin.Ban)
					return Ban(peer, admin);
				if (admin.Unban)
					return Unban(peer, admin);
				if (admin.ToggleMutePlayer)
					return ToggleMutePlayer(peer, admin);
				if (admin.Respawn)
					return Respawn(peer, admin);
				if (admin.ToggleInvulnerable)
					return ToggleInvulnerable(peer, admin);
				if (admin.TeleportToPlayer)
					return TeleportToPlayer(peer, admin);
				if (admin.TeleportPlayerToYou)
					return TeleportPlayerToYou(peer, admin);
				if (admin.TeleportAllPlayerToYou)
					return TeleportAllPlayerToYou(peer);
				if (admin.SendWarningToPlayer)
					return SendWarningToPlayer(peer, admin);
				if (admin.SetVIP)
					return SetVIP(peer, admin);
			}
			if (peer.IsSudo())
			{
				if (admin.SetSudo)
					return SetSudo(peer, admin);
				if (admin.SetAdmin)
					return SetAdmin(peer, admin);
			}

			return false;
		}

		public bool TeleportPlayerToYou(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			if (playerSelected == null) return false;

			Vec3 tpPosition = peer.ControlledAgent != null ?
				peer.ControlledAgent.Position :
				peer.GetCameraPosition();

			teleportPlayersToYou(new List<NetworkCommunicator> { playerSelected }, peer, tpPosition);

			Log($"[AdminPanel][TP] Player {playerSelected.UserName} teleported by admin {peer.UserName} to {tpPosition}", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[TP] Player {playerSelected.UserName} teleported by admin {peer.UserName} to {tpPosition}", AdminServerLog.ColorList.Success);
			return true;
		}

		public bool TeleportAllPlayerToYou(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playerSelected = GameNetwork.NetworkPeers.ToList();

			Vec3 tpPosition = peer.ControlledAgent != null ?
				peer.ControlledAgent.Position :
				peer.GetCameraPosition();

			teleportPlayersToYou(playerSelected, peer, tpPosition);

			Log($"[AdminPanel][TP] All players teleported by admin {peer.UserName} to {tpPosition}", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[TP] All players teleported by admin {peer.UserName} to {tpPosition}", AdminServerLog.ColorList.Success);
			return true;
		}

		public bool TeleportToPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			// Check if admin and target player both have an agent
			if (playerSelected == null) return false;

			Vec3 tpPosition = peer.ControlledAgent != null ?
				peer.ControlledAgent.Position :
				peer.GetCameraPosition();

			bool playerHasAgent = peer.ControlledAgent != null;
			bool targetHasAgent = playerSelected.ControlledAgent != null;

			if (playerHasAgent && targetHasAgent)
			{
				// Both have agents
				peer.ControlledAgent.TeleportToPosition(playerSelected.ControlledAgent.Position);
			}
			else if (playerHasAgent && !targetHasAgent)
			{
				// Only requester have agent
				peer.ControlledAgent.TeleportToPosition(playerSelected.GetCameraPosition());
			}
			else if (!playerHasAgent && targetHasAgent)
			{
				// Only target have agent
				ServerCoreMsg.SendClientCameraPosition(playerSelected.ControlledAgent.Frame, peer);
			}
			else if (!playerHasAgent && !targetHasAgent)
			{
				// None have agent
				var targetCameraFrame = playerSelected.GetCameraFrame();
				ServerCoreMsg.SendClientCameraPosition(targetCameraFrame, peer);
			}

			Log($"[AdminPanel][TP] Admin {peer.UserName} teleported to player {playerSelected.UserName} ({tpPosition})", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[TP] Admin {peer.UserName} teleported to player {playerSelected.UserName} ({tpPosition})", AdminServerLog.ColorList.Success);
			return true;
		}

		public bool HealPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			healPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel][HEAL] Player : {playerSelected?.UserName} healed by admin {peer.UserName}", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[HEAL] Player {playerSelected?.UserName} healed by {peer.UserName}", AdminServerLog.ColorList.Success);
			return true;
		}

		public bool Respawn(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();
			MissionPeer missionPeer = playerSelected.GetComponent<MissionPeer>();

			if (missionPeer.Team == Mission.Current.AttackerTeam || missionPeer.Team == Mission.Current.DefenderTeam)
			{
				Mission.Current.GetMissionBehavior<RespawnBehavior>().RespawnPlayer(playerSelected);
				Log($"[AdminPanel][RESPAWN] Player {playerSelected?.UserName} respawn by admin {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[RESPAWN] Player {playerSelected?.UserName} respawn by {peer.UserName}", AdminServerLog.ColorList.Success);
				return true;

			}
			else
			{
				Log($"[AdminPanel][RESPAWN] Error while respawning, player {playerSelected?.UserName} doesn't belong to a team", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[RESPAWN] Error while respawning, player {playerSelected?.UserName} doesn't belong to a team", AdminServerLog.ColorList.Danger);
				return false;
			}

		}

		public bool HealAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersSelected = GameNetwork.NetworkPeers.ToList();

			healPlayers(playersSelected, peer);

			Log($"[AdminPanel][HEAL] All players healed by admin {peer.UserName}.", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[HEAL] All players healed by admin {peer.UserName}.", AdminServerLog.ColorList.Success);
			return true;
		}

		public bool GodMod(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			godModPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel][GODMODE] Player {playerSelected.UserName} set to GODMODE by admin {peer.UserName}.", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[GODMODE] Player {playerSelected.UserName} set to GODMODE by admin {peer.UserName}", AdminServerLog.ColorList.Success);

			return true;
		}

		public bool GodModAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersSelected = GameNetwork.NetworkPeers.ToList();

			godModPlayers(playersSelected, peer);

			Log($"[AdminPanel][GODMODE] All players set to GODMODE by admin {peer.UserName}.", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[Serveur][GODMODE] All players set to GODMODE by admin {peer.UserName}.", AdminServerLog.ColorList.Success);

			return true;
		}

		public bool KillPlayers(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersToKill = GameNetwork.NetworkPeers.ToList();

			killPlayers(playersToKill, peer);

			Log($"[AdminPanel][KILL] All players killed by admin {peer.UserName}.", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[KILL] All players killed by admin {peer.UserName}.", AdminServerLog.ColorList.Success);
			return true;
		}

		public bool KillBots(NetworkCommunicator peer)
		{
			killBots(peer);

			Log($"[AdminPanel][KILL] All bots killed by admin {peer.UserName}.", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[KILL] All bots killed by admin {peer.UserName}.", AdminServerLog.ColorList.Success);
			return true;
		}

		public bool Kill(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			if (playerSelected == null) return true;

			killPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel][KILL] Player {playerSelected.UserName} killed by admin {peer.UserName}.", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[KILL] Player {playerSelected.UserName} killed by admin {peer.UserName}", AdminServerLog.ColorList.Success);

			return true;
		}

		public bool SendWarningToPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;
			admin.WarningMessageToPlayer = string.IsNullOrEmpty(admin.WarningMessageToPlayer) ? "You received a warning from admins" : admin.WarningMessageToPlayer;

			SecurityManager.WarnPlayer(playerSelected.VirtualPlayer.Id, playerSelected, admin.WarningMessageToPlayer);

			Log($"[AdminPanel][WARNING] Player {playerSelected.UserName} received a warning from {peer.UserName}, reason : {admin.WarningMessageToPlayer}.", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[WARNING] Player {playerSelected.UserName} received a warning from {peer.UserName}, reason : {admin.WarningMessageToPlayer}", AdminServerLog.ColorList.Success);
			return true;

		}

		public bool Kick(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null)
			{
				ServerAdminMenuMsg.SendMessageToAdmin(peer, "Player not found.", AdminServerLog.ColorList.Danger);
				return false;
			}

			SecurityManager.KickPlayer(playerSelected.VirtualPlayer.Id, playerSelected, "Kicked by " + peer.UserName);
			Log($"[AdminPanel][KICK] Player {playerSelected.UserName} kicked by admin {peer.UserName}", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[KICK] Player {playerSelected.UserName} kicked by admin {peer.UserName}", AdminServerLog.ColorList.Success);

			return true;
		}

		public bool Ban(NetworkCommunicator peer, AdminClient admin)
		{
			PlayerStore.Instance.AllPlayersData.TryGetValue(admin.PlayerSelected, out AL_PlayerData playerData);
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.FirstOrDefault(x => x.VirtualPlayer.Id == admin.PlayerSelected);
			string playerName = playerSelected?.UserName ?? playerData?.Name;

			try
			{
				//Prepare log entry
				string logEntry = $@"
========================================
[BAN ENTRY] {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Admin: {peer.UserName}
Admin Peer ID: {peer.VirtualPlayer.Id}
Banned Player: {playerName}
Player Peer ID: {admin.PlayerSelected}
Reason: {admin.BanReason}

========================================
				";

				// Check if file exist
				if (!System.IO.File.Exists(SubModule.BanHistoryFilePath))
				{
					// If not exist then create it with header
					string header = $@"=======================
BAN HISTORY
=======================
This file contains the complete history of all bans on this server.
Every entry is separated with a line of ====.

File creation date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

=======================
					";
					System.IO.File.WriteAllText(SubModule.BanHistoryFilePath, header);
				}

				//Add new log entry into the file
				System.IO.File.AppendAllText(SubModule.BanHistoryFilePath, logEntry);

				//Ban the player
				SecurityManager.BanPlayer(admin.PlayerSelected, playerSelected, admin.BanReason);

				//Notification
				string notificationMessage = $"[BAN] {playerName} banned by {peer.UserName}. Reason : {admin.BanReason}";
				Log("[AdminPanel]" + notificationMessage, LogLevel.Information);

				ServerAdminMenuMsg.SendMessageToAllAdmins(notificationMessage, AdminServerLog.ColorList.Success);

				return true;
			}
			catch (Exception ex)
			{
				Log($"Error when trying to ban {playerName}: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
				ServerAdminMenuMsg.SendMessageToAdmin(peer, $"Error when trying to ban {playerName}: {ex.Message}", AdminServerLog.ColorList.Danger);
				return false;
			}
		}

		public bool Unban(NetworkCommunicator peer, AdminClient admin)
		{
			if (!PlayerStore.Instance.AllPlayersData.TryGetValue(admin.PlayerSelected, out AL_PlayerData playerData))
			{
				ServerAdminMenuMsg.SendMessageToAdmin(peer, "Player not found.", AdminServerLog.ColorList.Danger);
				return false;
			}

			if (!playerData.IsBanned)
			{
				ServerAdminMenuMsg.SendMessageToAdmin(peer, "Player is not banned.", AdminServerLog.ColorList.Danger);
				return false;
			}

			try
			{
				//Prepare log entry
				string logEntry = $@"
========================================
[UNBAN ENTRY] {DateTime.Now:yyyy-MM-dd HH:mm:ss}
Admin: {peer.UserName}
Admin Peer ID: {peer.VirtualPlayer.Id}
Unbanned Player: {playerData.Name}
Player Peer ID: {playerData.Id}

========================================
				";

				// Check if file exist
				if (!System.IO.File.Exists(SubModule.BanHistoryFilePath))
				{
					// If not exist then create it with header
					string header = $@"=======================
BAN HISTORY
=======================
This file contains the complete history of all bans on this server.
Every entry is separated with a line of ====.

File creation date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}

=======================
					";
					System.IO.File.WriteAllText(SubModule.BanHistoryFilePath, header);
				}

				//Add new log entry into the file
				System.IO.File.AppendAllText(SubModule.BanHistoryFilePath, logEntry);

				//Unban the player
				SecurityManager.UnbanPlayer(playerData.Id);

				//Notification
				string notificationMessage = $"[UNBAN] {playerData.Name} unbanned by {peer.UserName}.";
				Log("[AdminPanel]" + notificationMessage, LogLevel.Information);

				ServerAdminMenuMsg.SendMessageToAllAdmins(notificationMessage, AdminServerLog.ColorList.Success);

				return true;
			}
			catch (Exception ex)
			{
				Log($"Error when trying to unban {playerData?.Name}: {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
				ServerAdminMenuMsg.SendMessageToAdmin(peer, $"Error when trying to unban {playerData?.Name}: {ex.Message}", AdminServerLog.ColorList.Danger);
				return false;
			}
		}

		public bool ToggleMutePlayer(NetworkCommunicator peer, AdminClient admin)
		{
			if (admin.PlayerSelected == PlayerId.Empty)
			{
				ServerAdminMenuMsg.SendMessageToAdmin(peer, "Player not found.", AdminServerLog.ColorList.Danger);
				return false;
			}

			PlayerStore.Instance.AllPlayersData.TryGetValue(admin.PlayerSelected, out AL_PlayerData playerData);
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.FirstOrDefault(x => x.VirtualPlayer.Id == admin.PlayerSelected);
			string playerName = playerSelected?.UserName ?? playerData?.Name;

			if (admin.PlayerSelected.IsMuted())
			{
				SecurityManager.UnmutePlayer(admin.PlayerSelected, playerSelected);
				if (playerSelected != null) CommonAdminMsg.SendNotificationToPeerAsServer(playerSelected, $"You have been unmuted by admins");
				Log($"[AdminPanel][UNMUTE] Player {playerName} has been unmuted by {peer.UserName}.", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[UNMUTE] Player {playerName} has been unmuted by {peer.UserName}", AdminServerLog.ColorList.Success);
			}
			else
			{
				SecurityManager.MutePlayer(admin.PlayerSelected, playerSelected);
				if (playerSelected != null) CommonAdminMsg.SendNotificationToPeerAsServer(playerSelected, $"You have been muted by admins!");
				Log($"[AdminPanel][MUTE] Player {playerName} has been muted by {peer.UserName}.", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[MUTE] Player {playerName} has been muted by {peer.UserName}", AdminServerLog.ColorList.Success);
			}

			return true;
		}

		public bool SetAdmin(NetworkCommunicator peer, AdminClient admin)
		{
			if (admin.PlayerSelected == PlayerId.Empty)
			{
				ServerAdminMenuMsg.SendMessageToAdmin(peer, "Player not found.", AdminServerLog.ColorList.Danger);
				return false;
			}

			PlayerStore.Instance.AllPlayersData.TryGetValue(admin.PlayerSelected, out AL_PlayerData playerData);
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.FirstOrDefault(x => x.VirtualPlayer.Id == admin.PlayerSelected);
			string playerName = playerSelected?.UserName ?? playerData?.Name;

			if (playerData != null && playerData.Admin)
			{
				SecurityManager.RevokeAdmin(admin.PlayerSelected, playerSelected);
				Log($"[AdminPanel][ADMIN] Player {playerName} removed from admins by {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[ADMIN] Player {playerName} removed from admins by {peer.UserName}", AdminServerLog.ColorList.Success);
			}
			else
			{
				SecurityManager.GrantAdmin(admin.PlayerSelected, playerSelected);
				Log($"[AdminPanel][ADMIN] Player {playerName} added to admins by {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[ADMIN] Player {playerName} added to admins by {peer.UserName}", AdminServerLog.ColorList.Success);
			}

			return true;
		}

		public bool SetSudo(NetworkCommunicator peer, AdminClient admin)
		{
			if (admin.PlayerSelected == PlayerId.Empty)
			{
				ServerAdminMenuMsg.SendMessageToAdmin(peer, "Player not found.", AdminServerLog.ColorList.Danger);
				return false;
			}

			PlayerStore.Instance.AllPlayersData.TryGetValue(admin.PlayerSelected, out AL_PlayerData playerData);
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.FirstOrDefault(x => x.VirtualPlayer.Id == admin.PlayerSelected);
			string playerName = playerSelected?.UserName ?? playerData?.Name;

			if (admin.PlayerSelected.IsSudo())
			{
				SecurityManager.RevokeSudo(admin.PlayerSelected, playerSelected);
				Log($"[AdminPanel][ADMIN] Player {playerName} removed from sudos by {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[ADMIN] Player {playerName} removed from sudos by {peer.UserName}", AdminServerLog.ColorList.Success);
			}
			else
			{
				SecurityManager.GrantSudo(admin.PlayerSelected, playerSelected);
				Log($"[AdminPanel][ADMIN] Player {playerName} added to sudos by {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[ADMIN] Player {playerName} added to sudos by {peer.UserName}", AdminServerLog.ColorList.Success);
			}

			return true;
		}

		public bool SetVIP(NetworkCommunicator peer, AdminClient admin)
		{
			if (admin.PlayerSelected == PlayerId.Empty)
			{
				ServerAdminMenuMsg.SendMessageToAdmin(peer, "Player not found.", AdminServerLog.ColorList.Danger);
				return false;
			}

			PlayerStore.Instance.AllPlayersData.TryGetValue(admin.PlayerSelected, out AL_PlayerData playerData);
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.FirstOrDefault(x => x.VirtualPlayer.Id == admin.PlayerSelected);
			string playerName = playerSelected?.UserName ?? playerData?.Name;

			if (admin.PlayerSelected.IsVIP())
			{
				SecurityManager.RevokeVIP(admin.PlayerSelected, playerSelected);
				Log($"[AdminPanel][ADMIN] Player {playerName} removed from VIPs by {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[ADMIN] Player {playerName} removed from VIPs by {peer.UserName}", AdminServerLog.ColorList.Success);
			}
			else
			{
				SecurityManager.GrantVIP(admin.PlayerSelected, playerSelected);
				Log($"[AdminPanel][ADMIN] Player {playerName} added to VIPs by {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[ADMIN] Player {playerName} added to VIPs by {peer.UserName}", AdminServerLog.ColorList.Success);
			}

			return true;
		}

		public bool ToggleInvulnerable(NetworkCommunicator peer, AdminClient admin)
		{
			if (admin.PlayerSelected == PlayerId.Empty)
			{
				MortalityState state = _invulnerable ? MortalityState.Mortal : MortalityState.Invulnerable;
				foreach (Agent agent in Mission.Current?.AllAgents)
				{
					agent.SetMortalityState(state);
				}
				_invulnerable = !_invulnerable;
				Log($"[AdminPanel][MISC] All agents ({Mission.Current?.AllAgents.Count}) set to {state} by admin {peer.UserName}", LogLevel.Information);
				ServerAdminMenuMsg.SendMessageToAllAdmins($"[MISC] All agents set to {state} by {peer.UserName}", AdminServerLog.ColorList.Success);
				return true;
			}

			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id == admin.PlayerSelected).FirstOrDefault();

			// Si le joueur existe mais ne contrôle pas d'agent
			if (playerSelected != null && playerSelected.ControlledAgent == null) return false;
			playerSelected.ControlledAgent.ToggleInvulnerable();
			Log($"[AdminPanel][MISC] Player {playerSelected.UserName} set to {playerSelected.ControlledAgent.CurrentMortalityState} by admin {peer.UserName}", LogLevel.Information);
			ServerAdminMenuMsg.SendMessageToAllAdmins($"[MISC] Player {playerSelected.UserName} set to {playerSelected.ControlledAgent.CurrentMortalityState} by {peer.UserName}", AdminServerLog.ColorList.Success);
			return true;
		}

		/// <summary>
		/// Tue les joueurs passés en paramètre (= 2000 dégats perçant à la tête)
		/// </summary>
		/// <param name="playersToKill">Liste des NetworkCommunicator à tuer</param>
		/// <param name="peer">NetworkCommunicator à l'origine de la demande, utile uniquement pour logguer en cas d'erreur</param>
		private void killPlayers(List<NetworkCommunicator> playersToKill, NetworkCommunicator peer = null)
		{
			try
			{
				int killedCount = 0;
				foreach (NetworkCommunicator playerToKill in playersToKill)
				{
					// Check si joueur existe et contrôle un agent
					if (playerToKill == null || playerToKill.ControlledAgent == null) continue;

					CoreUtils.TakeDamage(playerToKill.ControlledAgent, 2000, 2000f);
					killedCount++;
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel][KILL] Error while trying to kill players : ({e.Message})", LogLevel.Error);
				ServerAdminMenuMsg.SendMessageToAdmin(peer, $"[KILL] Error while trying to kill players.", AdminServerLog.ColorList.Danger);
			}
		}

		/// <summary>
		/// Liste et tue tous les Bots sur la mission en cours (= 2000 dégats perçant à la tête).
		/// Ne tue les montures que s'il n'y a aucun autre agent à tuer, et uniquement si elles n'ont pas de cavalier.
		/// Ignore les autres animaux (agents neutres, sans team et pas considérés comme montures).
		/// </summary>
		/// <param name="peer">NetworkCommunicator à l'origine de la demande, utile uniquement pour logguer en cas d'erreur</param>
		private void killBots(NetworkCommunicator peer = null)
		{
			try
			{
				List<Agent> agentsToKill = new List<Agent>();
				List<Agent> mountsToKill = new List<Agent>();

				foreach (var agent in Mission.Current.AllAgents)
				{
					if (agent == null || !agent.IsActive())
						continue;

					// Vérifie si l'agent est contrôlé par l'IA (donc pas un joueur)
					if (!agent.IsPlayerControlled && agent.Controller == AgentControllerType.AI)
					{
						if (!agent.IsMount && agent.Team != null)
						{
							agentsToKill.Add(agent);
						}
						else if (agent.IsMount && agent.RiderAgent?.MissionPeer == null)
						{
							mountsToKill.Add(agent);
						}
					}
				}

				foreach (var agent in agentsToKill)
				{
					CoreUtils.TakeDamage(agent, 2000, 2000f);
				}

				if (agentsToKill.Count == 0)
				{
					foreach (var agent in mountsToKill)
					{
						CoreUtils.TakeDamage(agent, 2000, 2000f);
					}
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel][KILL] Error while trying to kill bots : ({e.Message})", LogLevel.Error);
				if (peer != null)
				{
					ServerAdminMenuMsg.SendMessageToAdmin(peer, "[KILL] Error while trying to kill bots.", AdminServerLog.ColorList.Danger);
				}
			}

		}

		/// <summary>
		/// Passe en GodMod les joueurs passés en paramètre (= vie à 2000 et vitesse à 10)
		/// </summary>
		/// <param name="playersSelected">Liste des NetworkCommunicator à passer en GodMod</param>
		/// <param name="peer">NetworkCommunicator à l'origine de la demande, utile uniquement pour logguer en cas d'erreur</param>
		private void godModPlayers(List<NetworkCommunicator> playersSelected, NetworkCommunicator peer = null)
		{
			try
			{
				foreach (NetworkCommunicator playerSelected in playersSelected)
				{
					// Check si joueur existe et contrôle un agent
					if (playerSelected == null || playerSelected.ControlledAgent == null) continue;

					playerSelected.ControlledAgent.BaseHealthLimit = 2000;
					playerSelected.ControlledAgent.HealthLimit = 2000;
					playerSelected.ControlledAgent.Health = 2000;
					playerSelected.ControlledAgent.AgentDrivenProperties.MaxSpeedMultiplier = 10f;
					playerSelected.ControlledAgent.UpdateCustomDrivenProperties();
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel][GODMODE] Error while trying to set players to GODMODE. ({e.Message})", LogLevel.Error);
				ServerAdminMenuMsg.SendMessageToAdmin(peer, $"[GODMODE] Error while trying to set players to GODMODE.", AdminServerLog.ColorList.Danger);
			}
		}

		/// <summary>
		/// Soigne les joueurs passés en paramètre (= passage de la vie actuelle à la vie max)
		/// </summary>
		/// <param name="playersSelected">Liste des NetworkCommunicator à tuer</param>
		/// <param name="peer">NetworkCommunicator à l'origine de la demande, utile uniquement pour logguer en cas d'erreur</param>
		private void healPlayers(List<NetworkCommunicator> playersSelected, NetworkCommunicator peer = null)
		{
			try
			{
				foreach (NetworkCommunicator playerSelected in playersSelected)
				{
					// Check si joueur existe et contrôle un agent
					if (playerSelected == null || playerSelected.ControlledAgent == null) continue;

					playerSelected.ControlledAgent.Health = playerSelected.ControlledAgent.HealthLimit;
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel][HEAL] Error while trying to heal players. ({e.Message})", LogLevel.Error);
				ServerAdminMenuMsg.SendMessageToAdmin(peer, $"[HEAL] Error while trying to heal players.", AdminServerLog.ColorList.Danger);
			}
		}

		/// <summary>
		/// Teleporte les joueurs passés en paramètre au Networkcommunicator passé en paramètre.
		/// </summary>
		/// <param name="playersToTeleport">Tous les joueurs à téléporté</param>
		/// <param name="peer">Les joueurs seront téléportés à la position de ce joueur</param>
		private void teleportPlayersToYou(List<NetworkCommunicator> playersToTeleport, NetworkCommunicator peer, Vec3 tpPosition)
		{
			try
			{
				foreach (NetworkCommunicator playerToTeleport in playersToTeleport)
				{
					// Check if admin and target player both have an agent, also prevent admin from teleporting to himself
					if (playerToTeleport == null
						|| peer.VirtualPlayer.Id == playerToTeleport.VirtualPlayer.Id)
					{
						continue;
					}

					bool playerHasAgent = peer.ControlledAgent != null;
					bool targetHasAgent = playerToTeleport.ControlledAgent != null;

					if (playerHasAgent && targetHasAgent)
					{
						// Both have agents
						playerToTeleport.ControlledAgent.TeleportToPosition(tpPosition);
					}
					else if (playerHasAgent && !targetHasAgent)
					{
						// Only requester have agent
						var targetCameraFrame = peer.ControlledAgent.Frame;
						ServerCoreMsg.SendClientCameraPosition(targetCameraFrame, playerToTeleport);
					}
					else if (!playerHasAgent && targetHasAgent)
					{
						// Only target have agent
						playerToTeleport.ControlledAgent.TeleportToPosition(tpPosition);

					}
					else if (!playerHasAgent && !targetHasAgent)
					{
						// Move camera of target to camera of player
						var targetCameraFrame = peer.GetCameraFrame();
						ServerCoreMsg.SendClientCameraPosition(targetCameraFrame, playerToTeleport);
					}
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel][TP] Error while trying to teleport players : ({e.Message})", LogLevel.Error);
				ServerAdminMenuMsg.SendMessageToAdmin(peer, $"[TP] Error while trying to teleport players.", AdminServerLog.ColorList.Danger);
			}
		}
	}
}
