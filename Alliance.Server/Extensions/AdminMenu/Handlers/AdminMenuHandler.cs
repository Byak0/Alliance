using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Server.Core.Security;
using Alliance.Server.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using Alliance.Server.Extensions.AdminMenu.Behaviors;
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
			Log($"[AdminPanel] L'admin {peer.UserName} s'est téléporté en {req.Position}", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] L'admin {peer.UserName} s'est téléporté en {req.Position}", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool HandleNotificationRequest(NetworkCommunicator peer, RequestNotification notification)
		{
			if (peer.IsAdmin())
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SendNotification(notification.Text, notification.NotificationType));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
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
				if (admin.KillAll)
					return KillAll(peer);
				if (admin.Kick)
					return Kick(peer, admin);
				if (admin.Ban)
					return Ban(peer, admin);
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
			}
			if (peer.IsDev())
			{
				if (admin.SetAdmin)
					return SetAdmin(peer, admin);
			}

			return false;
		}

		public bool TeleportPlayerToYou(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			if (playerSelected == null) return false;

			teleportPlayersToYou(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel] Le joueur {playerSelected.UserName} a été téléporté par l'administrateur {peer.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été téléporté par l'administrateur {peer.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool TeleportAllPlayerToYou(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playerSelected = GameNetwork.NetworkPeers.ToList();

			teleportPlayersToYou(playerSelected, peer);

			Log($"[AdminPanel] Tous les joueurs ont été téléportés par l'administrateur {peer.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Tous les joueurs ont été téléportés par l'administrateur {peer.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool TeleportToPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check if admin and target player both have an agent
			if (playerSelected == null || playerSelected.ControlledAgent == null || peer.ControlledAgent == null) return false;

			peer.ControlledAgent.TeleportToPosition(playerSelected.ControlledAgent.Position);

			Log($"[AdminPanel] L'administrateur {peer.UserName} s'est téléporté sur le joueur {playerSelected.UserName} ({peer.ControlledAgent.Position})", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] L'administrateur {peer.UserName} s'est téléporté sur le joueur {playerSelected.UserName} ({peer.ControlledAgent.Position})", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool HealPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			healPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel] Le joueur : {playerSelected?.UserName} a été soigné par l'administrateur {peer.UserName}", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected?.UserName} est soigné par {peer.UserName}", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public  bool Respawn(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();
			MissionPeer missionPeer = playerSelected.GetComponent<MissionPeer>();

			if (missionPeer.Team == Mission.Current.AttackerTeam || missionPeer.Team == Mission.Current.DefenderTeam)
			{
				Mission.Current.GetMissionBehavior<RespawnBehavior>().RespawnPlayer(playerSelected);
				Log($"[AdminPanel] Le joueur : {playerSelected?.UserName} a été respawn par l'administrateur {peer.UserName}", LogLevel.Information);
				SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected?.UserName} est respawn par {peer.UserName}", AdminServerLog.ColorList.Success, true);
				return true; 
				
			}
			else
			{
				Log($"[AdminPanel] Erreur lors du respawn, le joueur n'est pas dans une équipe ", LogLevel.Information);
				SendMessageToClient(peer, $"[AdminPanel]  Erreur lors du respawn, le joueur n'est pas dans une équipe.", AdminServerLog.ColorList.Danger, true);
				return false;
			}

		}

		public bool HealAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersSelected = GameNetwork.NetworkPeers.ToList();

			healPlayers(playersSelected, peer);

			Log($"[AdminPanel] Tous les joueurs ont été soignés par l'administrateur {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Tous les joueurs ont été soignés par l'administrateur {peer.UserName}.", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool GodMod(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			godModPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel] Le joueur : {playerSelected.UserName} est en GodMod grâce à l'admin {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été mis en GodMod par {peer.UserName}", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool GodModAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersSelected = GameNetwork.NetworkPeers.ToList();

			godModPlayers(playersSelected, peer);

			Log($"[AdminPanel] Tous les joueurs entrent en GodMod grâce à l'admin {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Tous les joueurs entrent en GodMod grâce à l'admin {peer.UserName}.", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool KillAll(NetworkCommunicator peer)
		{
			List<NetworkCommunicator> playersToKill = GameNetwork.NetworkPeers.ToList();

			killPlayers(playersToKill, peer);

			Log($"[AdminPanel] Tous les joueurs ont été tués par l'admin {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Tous les joueurs ont été tués par l'admin {peer.UserName}.", AdminServerLog.ColorList.Success, true);
			return true;
		}

		public bool Kill(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			if (playerSelected == null) return true;

			killPlayers(new List<NetworkCommunicator> { playerSelected }, peer);

			Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été tué par l'admin {peer.UserName}.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été tué par {peer.UserName}", AdminServerLog.ColorList.Success, true);

			return true;
		}

		public bool SendWarningToPlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			GameNetwork.BeginModuleEventAsServer(playerSelected);
			GameNetwork.WriteMessage(new SendNotification($"{admin.WarningMessageToPlayer} (Admin : {peer.UserName}) !", 0));
			GameNetwork.EndModuleEventAsServer();

			Log($"[AdminPanel] Le joueur {playerSelected.UserName} a reçu un avertissement par {peer.UserName} avec la raison suivante : {admin.WarningMessageToPlayer}.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a reçu un avertissement par {peer.UserName} avec la raison suivante : {admin.WarningMessageToPlayer}", AdminServerLog.ColorList.Success, true);
			return true;

		}

		public bool Kick(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été kick.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été kick par {peer.UserName}", AdminServerLog.ColorList.Success, true);
			MissionPeer playerToKick = playerSelected.GetComponent<MissionPeer>();
			DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(playerToKick.Peer.Id, false);
			return true;
		}

		public bool Ban(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			MissionPeer playerToKick = playerSelected.GetComponent<MissionPeer>();

			Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été ban.", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été ban par {peer.UserName}", AdminServerLog.ColorList.Success);

			SecurityManager.AddBan(playerSelected.VirtualPlayer);

			DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(playerToKick.Peer.Id, false);

			return true;
		}

		public bool ToggleMutePlayer(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			if (playerSelected.IsMuted())
			{

				GameNetwork.BeginModuleEventAsServer(playerSelected);
				GameNetwork.WriteMessage(new SendNotification($"Vous avez été démute par un Admin ({peer.UserName}) !", 0));
				GameNetwork.EndModuleEventAsServer();

				Log($"[AdminPanel] Le joueur : {playerSelected.UserName} n'est plus mute.", LogLevel.Information);
				SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été retiré des jouers mués par {peer.UserName}", AdminServerLog.ColorList.Success, true);
				SecurityManager.RemoveMute(playerSelected.VirtualPlayer);
			}
			else
			{
				GameNetwork.BeginModuleEventAsServer(playerSelected);
				GameNetwork.WriteMessage(new SendNotification($"Vous avez été mute par un Admin ({peer.UserName}) !", 0));
				GameNetwork.EndModuleEventAsServer();

				Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été mute.", LogLevel.Information);
				SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été mute par {peer.UserName}", AdminServerLog.ColorList.Success, true);
				SecurityManager.AddMute(playerSelected.VirtualPlayer);
			}

			return true;
		}

		public bool SetAdmin(NetworkCommunicator peer, AdminClient admin)
		{
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Check si joueur existe
			if (playerSelected == null) return false;

			if (playerSelected.IsAdmin())
			{
				Log($"[AdminPanel] Le joueur : {playerSelected.UserName} n'est plus admin.", LogLevel.Information);
				SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été retiré des admins par {peer.UserName}", AdminServerLog.ColorList.Success, true);
				SecurityManager.RemoveAdmin(playerSelected.VirtualPlayer);
			}
			else
			{
				Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été promu admin.", LogLevel.Information);
				SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été promu admin par {peer.UserName}", AdminServerLog.ColorList.Success, true);
				SecurityManager.AddAdmin(playerSelected.VirtualPlayer);
			}

			return true;
		}

		public bool ToggleInvulnerable(NetworkCommunicator peer, AdminClient admin)
		{
			if (admin.PlayerSelected == "")
			{
				MortalityState state = _invulnerable ? MortalityState.Mortal : MortalityState.Invulnerable;
				foreach (Agent agent in Mission.Current?.AllAgents)
				{
					agent.SetMortalityState(state);
				}
				_invulnerable = !_invulnerable;
				Log($"[AdminPanel] Tous les agents ({Mission.Current?.AllAgents.Count}) ont été rendu {state.ToString()} par l'administrateur {peer.UserName}", LogLevel.Information);
				SendMessageToClient(peer, $"[Serveur] Tous les agents été rendu {state} par {peer.UserName}", AdminServerLog.ColorList.Success, true);
				return true;
			}

			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == admin.PlayerSelected).FirstOrDefault();

			// Si le joueur existe mais ne contrôle pas d'agent
			if (playerSelected != null && playerSelected.ControlledAgent == null) return false;
			playerSelected.ControlledAgent.ToggleInvulnerable();
			Log($"[AdminPanel] Le joueur : {playerSelected.UserName} a été rendu {playerSelected.ControlledAgent.CurrentMortalityState} par l'administrateur {peer.UserName}", LogLevel.Information);
			SendMessageToClient(peer, $"[Serveur] Le joueur {playerSelected.UserName} a été rendu {playerSelected.ControlledAgent.CurrentMortalityState} par {peer.UserName}", AdminServerLog.ColorList.Success, true);
			return true;
		}

		private void SendMessageToClient(NetworkCommunicator targetPeer, string message, AdminServerLog.ColorList color, bool forAdmin = false)
		{
			if (!forAdmin)
			{
				GameNetwork.BeginModuleEventAsServer(targetPeer);
				GameNetwork.WriteMessage(new AdminServerLog(message, color));
				GameNetwork.EndModuleEventAsServer();
			}

			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(new AdminServerLog(message, color));
				GameNetwork.EndModuleEventAsServer();
			}
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
				foreach (NetworkCommunicator playerToKill in playersToKill)
				{
					// Check si joueur existe et contrôle un agent
					if (playerToKill == null || playerToKill.ControlledAgent == null) continue;

					CoreUtils.TakeDamage(playerToKill.ControlledAgent, 2000, 2000f);
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel] Erreur lors de l'execution de la fonction killPlayers. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Erreur lors de l'execution de la fonction killPlayers.", AdminServerLog.ColorList.Danger, true);
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
				Log($"[AdminPanel] Erreur lors de l'execution de la fonction godModPlayers. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Erreur lors de l'execution de la fonction godModPlayers.", AdminServerLog.ColorList.Danger, true);
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
				Log($"[AdminPanel] Erreur lors de l'execution de la fonction healPlayers. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Erreur lors de l'execution de la fonction healPlayers.", AdminServerLog.ColorList.Danger, true);
			}
		}

		/// <summary>
		/// Teleporte les joueurs passés en paramètre au Networkcommunicator passé en paramètre.
		/// </summary>
		/// <param name="playersToTeleport">Tous les joueurs à téléporté</param>
		/// <param name="peer">Les joueurs seront téléportés à la position de ce joueur</param>
		private void teleportPlayersToYou(List<NetworkCommunicator> playersToTeleport, NetworkCommunicator peer)
		{
			try
			{
				foreach (NetworkCommunicator playerToTeleport in playersToTeleport)
				{
					// Check if admin and target player both have an agent, also prevent admin from teleporting to himself
					if (playerToTeleport == null
						|| playerToTeleport.ControlledAgent == null
						|| peer.ControlledAgent == null
						|| peer.VirtualPlayer.Id == playerToTeleport.VirtualPlayer.Id)
					{
						continue;
					}

					playerToTeleport.ControlledAgent.TeleportToPosition(peer.ControlledAgent.Position);
				}
			}
			catch (Exception e)
			{
				Log($"[AdminPanel] Erreur lors de l'execution de la fonction teleportPlayersToYou. ({e.Message})", LogLevel.Error);
				SendMessageToClient(peer, $"[AdminPanel] Erreur lors de l'execution de la fonction teleportPlayersToYou.", AdminServerLog.ColorList.Danger, true);
			}
		}

	}
}
