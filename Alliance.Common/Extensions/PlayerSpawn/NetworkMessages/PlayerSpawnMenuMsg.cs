using Alliance.Common.Core.Configuration.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Core.Configuration.Models.AllianceData;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages
{
	public static class PlayerSpawnMenuMsg
	{
		public static readonly CompressionInfo.Integer OperationCompressionInfo = new CompressionInfo.Integer(0, Enum.GetValues(typeof(PlayerSpawnMenuOperation)).Length, true);
		public static readonly CompressionInfo.Integer TeamIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);
		public static readonly CompressionInfo.Integer FormationIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);
		public static readonly CompressionInfo.Integer CharacterIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);

		#region Server Messages
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

		// From server - Sends the election timer to a specific peer.
		public static void SendElectionStatusToPeer(bool enable, float timer, NetworkCommunicator peer)
		{
			SendToClient(new SetElectionStatus(enable, timer), peer);
		}

		// From server - Broadcasts the election timer to all players in the game.
		public static void SendElectionStatusToAll(bool enable, float timer = 0f)
		{
			SendToClient(new SetElectionStatus(enable, timer));
		}

		// From server - Sends the spawn timer to a specific peer.
		public static void SendSpawnStatusToPeer(bool enable, float timer, NetworkCommunicator peer)
		{
			SendToClient(new SetSpawnStatus(enable, timer), peer);
		}

		// From server - Sends the spawn timer to all.
		public static void SendSpawnStatusToAll(bool enable, float timer)
		{
			SendToClient(new SetSpawnStatus(enable, timer));
		}

		// From server - A player has reserved a character - Send info to a specific peer.
		public static void SendAddPlayerCharacterUsageToPeer(NetworkCommunicator player, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter, NetworkCommunicator targetPeer)
		{
			SendToClient(new AddCharacterUsage(player, playerTeam, playerFormation, availableCharacter), targetPeer);
		}

		// From server - A player has reserved a character - Broadcast info to all players.
		public static void SendAddPlayerCharacterUsageToAll(NetworkCommunicator player, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter)
		{
			SendToClient(new AddCharacterUsage(player, playerTeam, playerFormation, availableCharacter));
		}

		// From server - A player no longer has a character reserved - Broadcast info to all players.
		public static void SendRemovePlayerCharacterUsageToAll(NetworkCommunicator player)
		{
			SendToClient(new RemoveCharacterUsage(player));
		}

		// From server - A player is candidate for officer - Send info to a specific peer.
		public static void SendAddOfficerCandidacyToPeer(NetworkCommunicator officerCandidate, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter, string pitch, NetworkCommunicator targetPeer)
		{
			SendToClient(new AddOfficerCandidacy(officerCandidate, playerTeam, playerFormation, availableCharacter, pitch), targetPeer);
		}

		// From server - A player is candidate for officer - Broadcast info to all players.
		public static void SendAddOfficerCandidacyToAll(NetworkCommunicator officerCandidate, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter, string pitch)
		{
			SendToClient(new AddOfficerCandidacy(officerCandidate, playerTeam, playerFormation, availableCharacter, pitch));
		}

		// From server - A player is no longer candidate for officer - Broadcast info to all players.
		public static void SendRemoveOfficerCandidacyToAll(NetworkCommunicator officerCandidate, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter)
		{
			SendToClient(new RemoveOfficerCandidacy(officerCandidate, playerTeam, playerFormation, availableCharacter));
		}

		// From server - Set a formation's officer - Broadcast info to all players.
		public static void SendSetFormationOfficerToAll(NetworkCommunicator newOfficer, PlayerTeam playerTeam, PlayerFormation playerFormation)
		{
			SendToClient(new SetFormationOfficer(newOfficer, playerTeam, playerFormation));
		}

		// From server - Set a formation's officer - Send info to a specific peer.
		public static void SendSetFormationOfficerToPeer(NetworkCommunicator newOfficer, PlayerTeam playerTeam, PlayerFormation playerFormation, NetworkCommunicator targetPeer)
		{
			SendToClient(new SetFormationOfficer(newOfficer, playerTeam, playerFormation), targetPeer);
		}

		// From server - Set a formation's main language - Broadcast info to all players.
		public static void SendFormationLanguageToAll(PlayerTeam team, PlayerFormation formation, string language)
		{
			int mainLanguageIndex = LocalizationHelper.GetAvailableLanguages().IndexOf(language);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncFormationMainLanguage(team, formation, mainLanguageIndex));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		private static void SendPlayerSpawnMenu(NetworkCommunicator player = null)
		{
			int messageCount = 0;
			SendToClient(new SyncPlayerSpawnMenu(GlobalOperation.BeginMenuSync), player);
			messageCount++;
			foreach (PlayerTeam team in PlayerSpawnMenu.Instance.Teams)
			{
				SendToClient(new SyncPlayerSpawnMenu(TeamOperation.AddTeam, team), player);
				messageCount++;
				foreach (PlayerFormation formation in team.Formations)
				{
					SendToClient(new SyncPlayerSpawnMenu(FormationOperation.AddFormation, team, formation), player);
					messageCount++;
					foreach (AvailableCharacter character in formation.AvailableCharacters)
					{
						SendToClient(new SyncPlayerSpawnMenu(CharacterOperation.AddCharacter, team, formation, character), player);
						messageCount++;
					}
				}
			}
			SendToClient(new SyncPlayerSpawnMenu(GlobalOperation.EndMenuSync), player);
			messageCount++;
			Log($"Alliance - Total messages sent to sync PlayerSpawnMenu: {messageCount}");
		}

		// Sends a message to the specified peer or broadcasts it to all clients if no peer is specified.
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
		#endregion

		#region Client Messages
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

		// From client - Request to reserve a character.
		public static void RequestCharacterUsage(PlayerTeam team, PlayerFormation formation, AvailableCharacter character, List<int> selectedPerks)
		{
			SendToServer(new RequestCharacterUsage(team, formation, character, selectedPerks));
		}

		// From client - Request to become an officer.
		public static void RequestOfficerUsage(PlayerTeam team, PlayerFormation formation, AvailableCharacter character, List<int> selectedPerks, string pitch)
		{
			SendToServer(new RequestOfficerUsage(team, formation, character, selectedPerks, pitch));
		}

		// From client - Request to vote (or remove vote) for an officer.
		public static void RequestVoteForOfficer(NetworkCommunicator player, bool addVote = true)
		{
			SendToServer(new VoteForOfficer(player, addVote));
		}

		private static void SendToServer(GameNetworkMessage message)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(message);
			GameNetwork.EndModuleEventAsClient();
		}
		#endregion

		#region Packet Read/Write Methods - Utilities
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
		#endregion
	}
}
