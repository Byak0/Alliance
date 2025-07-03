using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMessage;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Utilities
{
	public static class PlayerSpawnMenuNetworkHelper
	{
		public static void SendPlayerSpawnMenuToPeer(NetworkCommunicator peer)
		{
			if (!GameNetwork.IsServer) return;
			Log($"Alliance - Sending PlayerSpawnMenu to {peer.UserName}...");
			SendPlayerSpawnMenu(peer);
		}

		public static void SendPlayerSpawnMenuToAll()
		{
			if (!GameNetwork.IsServer) return;
			Log("Alliance - Broadcasting PlayerSpawnMenu to all players...");
			SendPlayerSpawnMenu();
		}

		public static void RequestUpdatePlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu)
		{
			if (!GameNetwork.IsClient) return;
			Log("Alliance - Sending PlayerSpawnMenu to server...");
			int messageCount = 0;
			SendToServer(new UpdatePlayerSpawnMenu(GlobalOperation.BeginMenuSync));
			messageCount++;
			foreach (PlayerTeam team in playerSpawnMenu.Teams)
			{
				SendToServer(new UpdatePlayerSpawnMenu(TeamOperation.AddTeam, team));
				messageCount++;
				foreach (PlayerFormation formation in team.Formations)
				{
					SendToServer(new UpdatePlayerSpawnMenu(FormationOperation.AddFormation, team, formation));
					messageCount++;
					foreach (AvailableCharacter character in formation.AvailableCharacters)
					{
						SendToServer(new UpdatePlayerSpawnMenu(CharacterOperation.AddCharacter, team, formation, character));
						messageCount++;
					}
				}
			}
			SendToServer(new UpdatePlayerSpawnMenu(GlobalOperation.EndMenuSync));
			messageCount++;
			Log($"Alliance - Total messages sent to update PlayerSpawnMenu: {messageCount}");
		}

		private static void SendPlayerSpawnMenu(NetworkCommunicator peer = null)
		{
			int messageCount = 0;
			SendToClient(new SyncPlayerSpawnMenu(GlobalOperation.BeginMenuSync), peer);
			messageCount++;
			foreach (PlayerTeam team in PlayerSpawnMenu.Instance.Teams)
			{
				SendToClient(new SyncPlayerSpawnMenu(TeamOperation.AddTeam, team), peer);
				messageCount++;
				foreach (PlayerFormation formation in team.Formations)
				{
					SendToClient(new SyncPlayerSpawnMenu(FormationOperation.AddFormation, team, formation), peer);
					messageCount++;
					foreach (AvailableCharacter character in formation.AvailableCharacters)
					{
						SendToClient(new SyncPlayerSpawnMenu(CharacterOperation.AddCharacter, team, formation, character), peer);
						messageCount++;
					}
				}
			}
			SendToClient(new SyncPlayerSpawnMenu(GlobalOperation.EndMenuSync), peer);
			messageCount++;
			Log($"Alliance - Total messages sent to sync PlayerSpawnMenu: {messageCount}");
		}

		private static void SendToClient(GameNetworkMessage message, NetworkCommunicator peer = null)
		{
			if (peer == null)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(message);
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			else
			{
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(message);
				GameNetwork.EndModuleEventAsServer();
			}
		}

		private static void SendToServer(GameNetworkMessage message)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(message);
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
