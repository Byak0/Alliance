using Alliance.Common.Core.Configuration.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Core.Configuration.Models.AllianceData;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Utilities
{
	public static class PlayerSpawnMenuNetworkHelper
	{
		public static readonly CompressionInfo.Integer OperationCompressionInfo = new CompressionInfo.Integer(0, Enum.GetValues(typeof(PlayerSpawnMenuOperation)).Length, true);
		public static readonly CompressionInfo.Integer TeamIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);
		public static readonly CompressionInfo.Integer FormationIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);
		public static readonly CompressionInfo.Integer CharacterIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);

		/// <summary>
		/// From server - Sends the PlayerSpawnMenu to a specific peer.
		/// </summary>
		public static void SendPlayerSpawnMenuToPeer(NetworkCommunicator peer)
		{
			if (!GameNetwork.IsServer) return;
			Log($"Alliance - Sending PlayerSpawnMenu to {peer.UserName}...");
			SendPlayerSpawnMenu(peer);
		}

		/// <summary>
		/// From server - Broadcasts the PlayerSpawnMenu to all players in the game.
		/// </summary>
		public static void SendPlayerSpawnMenuToAll()
		{
			if (!GameNetwork.IsServer) return;
			Log("Alliance - Broadcasting PlayerSpawnMenu to all players...");
			SendPlayerSpawnMenu();
		}

		/// <summary>
		/// From client - Send a new PlayerSpawnMenu for the server to use.
		/// </summary>
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

		public static void WritePlayerTeamToPacket(PlayerTeam team)
		{
			GameNetworkMessage.WriteIntToPacket(team.Index, TeamIndexCompressionInfo);
			GameNetworkMessage.WriteStringToPacket(team.Name);
			GameNetworkMessage.WriteIntToPacket((int)team.TeamSide, CompressionMission.TeamSideCompressionInfo);
		}

		public static PlayerTeam ReadPlayerTeamFromPacket(ref bool bufferReadValid)
		{
			PlayerTeam team = new PlayerTeam();
			team.Index = GameNetworkMessage.ReadIntFromPacket(TeamIndexCompressionInfo, ref bufferReadValid);
			team.Name = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
			team.TeamSide = (BattleSideEnum)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
			return team;
		}

		public static void WritePlayerFormationToPacket(PlayerFormation formation)
		{
			GameNetworkMessage.WriteIntToPacket(formation.Index, FormationIndexCompressionInfo);
			GameNetworkMessage.WriteStringToPacket(formation.Name);
			GameNetworkMessage.WriteObjectReferenceToPacket(formation.MainCulture, CompressionBasic.GUIDCompressionInfo);
			GameNetworkMessage.WriteBoolToPacket(formation.Settings.UseMorale);
		}

		public static PlayerFormation ReadPlayerFormationFromPacket(ref bool bufferReadValid)
		{
			PlayerFormation formation = new PlayerFormation();
			formation.Index = GameNetworkMessage.ReadIntFromPacket(FormationIndexCompressionInfo, ref bufferReadValid);
			formation.Name = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
			object cultureObject = GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
			if (cultureObject != null && cultureObject is BasicCultureObject bco)
			{
				formation.MainCultureId = bco.StringId;
			}
			formation.Settings.UseMorale = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			return formation;
		}

		public static void WriteAvailableCharacterToPacket(AvailableCharacter character)
		{
			GameNetworkMessage.WriteIntToPacket(character.Index, CompressionHelper.DefaultIntValueCompressionInfo);
			GameNetworkMessage.WriteObjectReferenceToPacket(character.Character, CompressionBasic.GUIDCompressionInfo);
			GameNetworkMessage.WriteBoolToPacket(character.Officer);
			GameNetworkMessage.WriteIntToPacket(character.SpawnCount, CompressionHelper.DefaultIntValueCompressionInfo);
			GameNetworkMessage.WriteBoolToPacket(character.IsPercentage);
			GameNetworkMessage.WriteIntToPacket((int)character.Difficulty, CompressionHelper.DefaultIntValueCompressionInfo);
			GameNetworkMessage.WriteFloatToPacket(character.HealthMultiplier, CompressionHelper.DefaultFloatValueCompressionInfo);
		}

		public static AvailableCharacter ReadAvailableCharacterFromPacket(ref bool bufferReadValid)
		{
			AvailableCharacter character = new AvailableCharacter();
			character.Index = GameNetworkMessage.ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			character.CharacterId = ((BasicCharacterObject)GameNetworkMessage.ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid)).StringId;
			character.Officer = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			character.SpawnCount = GameNetworkMessage.ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			character.IsPercentage = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			character.Difficulty = (Difficulty)GameNetworkMessage.ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			character.HealthMultiplier = GameNetworkMessage.ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid);
			return character;
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

		public enum PlayerSpawnMenuOperation
		{
			BeginMenuSync,
			EndMenuSync,
			AddTeam,
			UpdateTeam,
			RemoveTeam,
			AddFormation,
			UpdateFormation,
			RemoveFormation,
			AddCharacter,
			UpdateCharacter,
			RemoveCharacter
		}

		public enum GlobalOperation
		{
			BeginMenuSync = PlayerSpawnMenuOperation.BeginMenuSync,
			EndMenuSync = PlayerSpawnMenuOperation.EndMenuSync
		}

		public enum TeamOperation
		{
			AddTeam = PlayerSpawnMenuOperation.AddTeam,
			UpdateTeam = PlayerSpawnMenuOperation.UpdateTeam,
			RemoveTeam = PlayerSpawnMenuOperation.RemoveTeam
		}

		public enum FormationOperation
		{
			AddFormation = PlayerSpawnMenuOperation.AddFormation,
			UpdateFormation = PlayerSpawnMenuOperation.UpdateFormation,
			RemoveFormation = PlayerSpawnMenuOperation.RemoveFormation
		}

		public enum CharacterOperation
		{
			AddCharacter = PlayerSpawnMenuOperation.AddCharacter,
			UpdateCharacter = PlayerSpawnMenuOperation.UpdateCharacter,
			RemoveCharacter = PlayerSpawnMenuOperation.RemoveCharacter
		}
	}
}
