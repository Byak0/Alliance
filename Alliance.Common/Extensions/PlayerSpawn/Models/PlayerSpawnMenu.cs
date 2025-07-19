using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Models
{
	public class PlayerAssignment
	{
		public NetworkCommunicator Player;
		public PlayerTeam Team;
		public PlayerFormation Formation;
		public AvailableCharacter Character;
		public List<int> Perks;
		public float TimeBeforeSpawn;
		public bool CanSpawn;

		public PlayerAssignment(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation, List<int> perks, float timeBeforeSpawn, bool canSpawn)
		{
			Player = player;
			Team = team;
			Formation = formation;
			Perks = perks;
			TimeBeforeSpawn = timeBeforeSpawn;
			CanSpawn = canSpawn;
		}
	}

	[Serializable]
	public class PlayerSpawnMenu
	{
		// XML-serializable properties
		public List<PlayerTeam> Teams { get; set; } = new();

		// Runtime properties
		[XmlIgnore]
		public bool ElectionInProgress { get; private set; } = false;
		[XmlIgnore]
		public float TimeBeforeOfficerElection { get; set; } = 0f;

		// Lookup properties
		[XmlIgnore]
		private readonly Dictionary<NetworkCommunicator, PlayerAssignment> _playerAssignments = new();
		[XmlIgnore]
		public Dictionary<NetworkCommunicator, PlayerAssignment> PlayerAssignments => _playerAssignments;
		[XmlIgnore]
		public PlayerAssignment MyAssignment => GetPlayerAssignment(GameNetwork.MyPeer);

		// Events for UI updates (static because the instance is subject to change)
		public static event Action<NetworkCommunicator, PlayerTeam, PlayerFormation, AvailableCharacter> OnCharacterSelected;
		public static event Action<NetworkCommunicator, PlayerTeam, PlayerFormation, AvailableCharacter> OnCharacterDeselected;
		public static event Action<PlayerTeam, PlayerFormation> OnOfficerCandidaciesUpdated;
		public static event Action<bool> OnElectionStatusChanged;
		public static event Action<bool> OnSpawnStatusChanged;

		public static PlayerSpawnMenu Instance { get; set; } = new PlayerSpawnMenu();

		public PlayerAssignment GetPlayerAssignment(NetworkCommunicator player)
		{
			if (player == null) return null;
			if (_playerAssignments.TryGetValue(player, out var assignment))
			{
				return assignment;
			}
			_playerAssignments[player] = new PlayerAssignment(player, null, null, new List<int>(), -1f, false);
			return _playerAssignments[player];
		}

		public void StartOfficerElection(float timeBeforeElection)
		{
			ElectionInProgress = true;
			TimeBeforeOfficerElection = timeBeforeElection;
			OnElectionStatusChanged?.Invoke(true);
			Log($"Officer election started, will end in {timeBeforeElection} seconds.", LogLevel.Debug);
		}

		public void EndElection()
		{
			ElectionInProgress = false;
			TimeBeforeOfficerElection = 0f;
			OnElectionStatusChanged?.Invoke(false);
			if (GameNetwork.IsServer)
			{
				foreach (PlayerTeam team in Teams)
				{
					foreach (PlayerFormation formation in team.Formations)
					{
						ElectOfficer(team, formation);
					}
				}
			}
			Log("Officer election ended.", LogLevel.Debug);
		}

		public void StartSpawnTimer(float timeBeforeSpawn)
		{
			//SpawnEnabled = true;
			if (MyAssignment != null)
			{
				MyAssignment.CanSpawn = true;
				MyAssignment.TimeBeforeSpawn = timeBeforeSpawn;
				OnSpawnStatusChanged?.Invoke(true);
				Log($"You will spawn in {timeBeforeSpawn} seconds.", LogLevel.Debug);
			}
		}

		public void EndSpawn()
		{
			//SpawnEnabled = false;
			if (MyAssignment != null)
			{
				MyAssignment.CanSpawn = false;
				MyAssignment.TimeBeforeSpawn = -1f;
				OnSpawnStatusChanged?.Invoke(false);
				Log($"Spawn ended.", LogLevel.Debug);
			}
		}

		public PlayerTeam AddTeam(BattleSideEnum side, string name)
		{
			PlayerTeam team = new PlayerTeam()
			{
				Index = GetNextTeamIndex(Teams),
				TeamSide = side,
				Name = name
			};
			Teams.Add(team);
			return team;
		}

		public void RemoveTeam(PlayerTeam team)
		{
			if (team == null || !Teams.Contains(team)) return;
			// Remove all formations from this team
			foreach (PlayerFormation formation in team.Formations.ToList())
			{
				team.RemoveFormation(formation);
			}
			Teams.Remove(team);
		}

		public void SelectTeam(PlayerTeam team = null)
		{
			// Default to native team if no team is provided
			if (team == null)
			{
				MissionPeer myPeer = GameNetwork.MyPeer?.GetComponent<MissionPeer>();
				BattleSideEnum mySide = myPeer?.Team?.Side ?? BattleSideEnum.Defender;
				MyAssignment.Team = Teams.FirstOrDefault(t => t.TeamSide == mySide);
			}
			else
			{
				MyAssignment.Team = team;
			}
		}

		public void MovePlayerToFormation(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation)
		{
			if (player == null || team == null || formation == null)
			{
				return;
			}
			PlayerAssignment assignment = GetPlayerAssignment(player);
			if (assignment.Formation != null) assignment.Formation?.RemoveMember(player);
			assignment.Team = team;
			assignment.Formation = formation;
			// Add player to the new formation
			formation.AddMember(player);

			// Refresh formation language
			if (GameNetwork.IsServer)
			{
				string oldLanguage = formation.MainLanguage;
				formation.RefreshMainLanguage();
				if (oldLanguage != formation.MainLanguage)
				{
					PlayerSpawnMenuMsg.SendFormationLanguageToAll(team, formation, formation.MainLanguage);
				}
			}
		}

		public void UpdatePerks(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation, AvailableCharacter character, List<int> selectedPerks)
		{
			PlayerAssignment assignment = GetPlayerAssignment(player);

			if (assignment.Team != team || assignment.Formation != formation || assignment.Character != character)
			{
				Log($"Trying to update perks for the wrong character/formation", LogLevel.Error);
				return;
			}

			assignment.Perks = selectedPerks;
		}

		/// <summary>
		/// Elects an officer for the given team and formation based on the votes received by candidates.
		/// Elected officer's character will be set as the officer of the formation. Other candidates will be dispatched to characters with available slots.
		/// </summary>
		public void ElectOfficer(PlayerTeam team, PlayerFormation formation)
		{
			CandidateInfo electedCandidate = null;
			int maxVotes = 0;
			foreach (CandidateInfo candidate in formation.Candidates)
			{
				if (candidate.Votes > maxVotes)
				{
					maxVotes = candidate.Votes;
					electedCandidate = candidate;
				}
			}
			if (electedCandidate != null)
			{
				formation.SetOfficer(electedCandidate.Candidate);
				PlayerSpawnMenuMsg.SendSetFormationOfficerToAll(electedCandidate.Candidate, team, formation);
				// todo add synchronization message 
				Log($"Officer elected: {formation.Officer.UserName} with {maxVotes} votes.", LogLevel.Information);

				// Set officer's character
				SelectCharacter(electedCandidate.Candidate, team, formation, GetPlayerAssignment(formation.Officer).Character);
				// Notify all players about the character usage
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new AddCharacterUsage(electedCandidate.Candidate, team, formation, GetPlayerAssignment(formation.Officer).Character));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

				// Set the other candidates to characters with available slots
				foreach (CandidateInfo candidate in formation.Candidates)
				{
					if (candidate == electedCandidate) continue; // Skip the elected officer
					if (formation.Members.Contains(candidate.Candidate))
					{
						AvailableCharacter availableCharacter = formation.AvailableCharacters.FirstOrDefault(c => c.AvailableSlots > 0);
						if (availableCharacter != null)
						{
							SelectCharacter(candidate.Candidate, team, formation, availableCharacter);
							Log($"Candidate {candidate.Candidate.UserName} assigned to character {availableCharacter.Name}.", LogLevel.Information);
							// Notify all players about the character usage
							GameNetwork.BeginBroadcastModuleEvent();
							GameNetwork.WriteMessage(new AddCharacterUsage(candidate.Candidate, team, formation, availableCharacter));
							GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
						}
						else
						{
							Log($"No available character slots for candidate {candidate.Candidate.UserName}.", LogLevel.Warning);
						}
					}
				}
			}
			else
			{
				formation.SetOfficer(formation.Members.GetRandomElementInefficiently());
				// todo add synchronization message 
				Log($"Officer randomly selected : {formation.Officer?.UserName} (no candidate found)", LogLevel.Information);
			}
		}

		public void ClearCharacterSelection(NetworkCommunicator player)
		{
			PlayerAssignment assignment = GetPlayerAssignment(player);
			// Free previous character slot if any
			if (assignment.Character != null)
			{
				if (GameNetwork.IsServer) PlayerSpawnMenuMsg.SendRemovePlayerCharacterUsageToAll(player);

				assignment.Character.UsedSlots--;
				OnCharacterDeselected?.Invoke(player, assignment.Team, assignment.Formation, assignment.Character);
				assignment.Character = null;
			}
		}

		public bool TrySelectCharacter(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation, AvailableCharacter character, ref string _failReason)
		{
			if (!formation.AvailableCharacters.Contains(character))
			{
				_failReason = $"Character {character.Name} is not available in formation {formation.Name} or has no available slots.";
				return false;
			}

			if (character.AvailableSlots <= 0)
			{
				_failReason = $"Character {character.Name} has no available slots.";
				return false;
			}

			SelectCharacter(player, team, formation, character);

			return true;
		}

		public void SelectCharacter(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation, AvailableCharacter character)
		{
			//if (GameNetwork.IsServer) ClearDisconnectedPlayers(); // todo move to a better place, maybe in a periodic update or when players are added/removed

			ClearCharacterSelection(player);

			PlayerAssignment assignment = GetPlayerAssignment(player);
			// Add player to formation if not already
			if (!formation.Members.Contains(player) || assignment.Formation != formation)
			{
				MovePlayerToFormation(player, team, formation);
			}

			assignment.Character = character;
			character.UsedSlots++;

			OnCharacterSelected?.Invoke(player, team, formation, character);
		}

		public void UpdateSpawnStatus(NetworkCommunicator peer, bool canSpawn, float timeBeforeSpawn)
		{
			if (peer == null || !peer.IsConnectionActive)
			{
				Log("Cannot update spawn status for a null or disconnected peer.", LogLevel.Error);
				return;
			}
			PlayerAssignment assignment = GetPlayerAssignment(peer);
			assignment.CanSpawn = canSpawn;
			assignment.TimeBeforeSpawn = timeBeforeSpawn;
			PlayerSpawnMenuMsg.SendSpawnStatusToPeer(canSpawn, timeBeforeSpawn, peer);
		}

		// todo : find a way to clear disconnected players on client side too (send a message from server ?)
		public void ClearDisconnectedPlayers()
		{
			List<NetworkCommunicator> playersToRemove = new();

			foreach (KeyValuePair<NetworkCommunicator, PlayerAssignment> kvp in _playerAssignments)
			{
				if (!kvp.Key.IsConnectionActive) // Seems to be always true on client side so only check on server side
				{
					playersToRemove.Add(kvp.Key);
				}
			}

			foreach (NetworkCommunicator player in playersToRemove)
			{
				ClearPlayer(player);
			}
		}

		public void ClearPlayer(NetworkCommunicator player)
		{
			if (!_playerAssignments.TryGetValue(player, out PlayerAssignment assignment)) return;

			if (assignment.Character != null)
			{
				assignment.Character.UsedSlots--;
				OnCharacterDeselected?.Invoke(player, assignment.Team, assignment.Formation, assignment.Character);
			}

			if (assignment.Formation != null && assignment.Formation.Members.Contains(player))
			{
				assignment.Formation.RemoveMember(player);
			}

			_playerAssignments.Remove(player);
			Log($"Removed {player.UserName} assignment", LogLevel.Debug);
		}

		public void RefreshIndices()
		{
			int nextTeamIndex = 0;
			foreach (PlayerTeam team in Teams)
			{
				team.Index = nextTeamIndex++;
				int nextFormationIndex = 0;
				foreach (PlayerFormation formation in team.Formations)
				{
					formation.Index = nextFormationIndex++;
					int nextCharacterIndex = 0;
					foreach (AvailableCharacter character in formation.AvailableCharacters)
					{
						character.Index = nextCharacterIndex++;
					}
				}
			}
		}

		public static int GetNextTeamIndex(List<PlayerTeam> teams)
		{
			int index = 0;
			while (teams.Any(t => t.Index == index))
				index++;
			return index++;
		}

		public static int GetNextFormationIndex(PlayerTeam team)
		{
			int index = 0;
			while (team.Formations.Any(f => f.Index == index))
				index++;
			return index;
		}

		public static int GetNextCharacterIndex(PlayerFormation formation)
		{
			int index = 0;
			while (formation.AvailableCharacters.Any(c => c.Index == index))
				index++;
			return index;
		}

		/// <summary>
		/// Try loading the PlayerSpawnMenu from file. Returns true if successful, false otherwise.
		/// </summary>
		public static bool TryLoadFromFile(string fileName, out PlayerSpawnMenu playerSpawnMenu)
		{
			playerSpawnMenu = null;

			// Load the selected file
			string filePath = Path.GetFullPath(Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), "Spawn_Presets", fileName));
			try
			{
				if (File.Exists(filePath))
				{
					Log($"Loading PlayerSpawnMenu from {filePath}");
					playerSpawnMenu = SerializeHelper.LoadClassFromFile(filePath, new PlayerSpawnMenu());
					playerSpawnMenu.RefreshIndices(); // Ensure indices are unique and valid
					return true;
				}
				else
				{
					Log($"Can't load PlayerSpawnMenu, file doesn't exist : {filePath}", LogLevel.Error);
				}
			}
			catch (Exception ex)
			{
				Log($"Failed to load PlayerSpawnMenu from {filePath}: {ex.Message}", LogLevel.Error);
			}
			return false;
		}

		/// <summary>
		/// Try to save the PlayerSpawnMenu to file. Returns true if successful, false otherwise.
		/// </summary>
		public bool SaveToFile(string fileName)
		{
			string filePath = Path.GetFullPath(Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), "Spawn_Presets", fileName));
			try
			{
				SerializeHelper.SaveClassToFile(filePath, this);
				Log($"PlayerSpawnMenu saved to {filePath}", LogLevel.Information);
				return true;
			}
			catch (Exception ex)
			{
				Log($"Failed to save PlayerSpawnMenu to {filePath}: {ex.Message}", LogLevel.Error);
			}
			return false;
		}
	}
}
