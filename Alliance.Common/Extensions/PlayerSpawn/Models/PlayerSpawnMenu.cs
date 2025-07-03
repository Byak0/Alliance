using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Models
{
	[Serializable]
	public class PlayerSpawnMenu
	{
		// XML-serializable teams and formations
		public List<PlayerTeam> Teams { get; set; } = new();

		// Lookup dictionaries
		[XmlIgnore]
		private readonly Dictionary<NetworkCommunicator, PlayerTeam> _playerToTeam = new();
		public IReadOnlyDictionary<NetworkCommunicator, PlayerTeam> PlayerTeams => _playerToTeam;
		[XmlIgnore]
		private readonly Dictionary<NetworkCommunicator, PlayerFormation> _playerToFormation = new();
		public IReadOnlyDictionary<NetworkCommunicator, PlayerFormation> PlayerFormations => _playerToFormation;
		[XmlIgnore]
		private readonly Dictionary<NetworkCommunicator, AvailableCharacter> _playerToCharacter = new();
		public IReadOnlyDictionary<NetworkCommunicator, AvailableCharacter> PlayerCharacters => _playerToCharacter;

		[XmlIgnore]
		private PlayerTeam _selectedTeam;
		public PlayerTeam SelectedTeam => _selectedTeam;
		[XmlIgnore]
		private PlayerFormation _selectedFormation;
		public PlayerFormation SelectedFormation => _selectedFormation;
		[XmlIgnore]
		private AvailableCharacter _selectedCharacter;
		public AvailableCharacter SelectedCharacter => _selectedCharacter;

		public static PlayerSpawnMenu Instance { get; set; } = new PlayerSpawnMenu();

		public PlayerTeam AddTeam(BattleSideEnum side, string name)
		{
			PlayerTeam team = new PlayerTeam()
			{
				Index = GetNextTeamIndex(),
				TeamSide = side,
				Name = name
			};
			Teams.Add(team);
			return team;
		}

		public PlayerFormation AddFormation(PlayerTeam team, string name, FormationSettings settings = null, List<AvailableCharacter> chars = null)
		{
			PlayerFormation formation = new PlayerFormation()
			{
				Index = GetNextFormationIndex(team),
				Name = name,
				Settings = settings ?? new FormationSettings(),
				AvailableCharacters = chars ?? new List<AvailableCharacter>()
			};
			team.Formations.Add(formation);
			return formation;
		}

		public AvailableCharacter AddCharacter(PlayerFormation formation, string characterId, bool officer)
		{
			AvailableCharacter character = new AvailableCharacter
			{
				CharacterId = characterId,
				Officer = officer
			};
			return AddCharacter(formation, character);
		}

		public AvailableCharacter AddCharacter(PlayerFormation formation, AvailableCharacter character)
		{
			character.Index = GetNextCharacterIndex(formation);
			formation.AvailableCharacters.Add(character);
			return character;
		}

		public void RemoveTeam(PlayerTeam team)
		{
			if (team == null || !Teams.Contains(team)) return;
			// Remove all formations from this team
			foreach (PlayerFormation formation in team.Formations.ToList())
			{
				RemoveFormation(team, formation);
			}
			Teams.Remove(team);
		}

		public void RemoveFormation(PlayerTeam team, PlayerFormation formation)
		{
			if (team == null || formation == null) return;
			if (!team.Formations.Contains(formation)) return;

			// Remove all players from this formation
			foreach (NetworkCommunicator player in formation.Members.ToList())
			{
				RemovePlayerFromFormation(formation, player);
			}
			team.Formations.Remove(formation);
		}

		public void SelectTeam(PlayerTeam team)
		{
			// Default to native team if no team is provided
			if (team == null)
			{
				BattleSideEnum mySide = GameNetwork.MyPeer?.GetComponent<MissionPeer>()?.Team?.Side ?? BattleSideEnum.Defender;
				_selectedTeam = Teams.FirstOrDefault(t => t.TeamSide == mySide);
			}
			else
			{
				_selectedTeam = team;
			}
		}

		public void SelectFormation(PlayerFormation formation)
		{
			if (_selectedTeam == null)
			{
				Log("No team selected. Please select a team first.", LogLevel.Error);
				return;
			}
			if (formation != null && !_selectedTeam.Formations.Contains(formation))
			{
				Log($"Formation {formation?.Name} not found in team {_selectedTeam.Name}", LogLevel.Error);
				return;
			}
			_selectedFormation = formation;
		}

		public void MovePlayerToFormation(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation)
		{
			if (player == null || team == null || formation == null)
			{
				return;
			}
			if (_playerToFormation.TryGetValue(player, out var oldFormation))
			{
				RemovePlayerFromFormation(oldFormation, player);
			}
			formation.Members.Add(player);
			_playerToFormation[player] = formation;
		}

		public bool SelectCharacter(NetworkCommunicator player, AvailableCharacter character)
		{
			if (player == null) return false;
			// Check if player is in a formation
			if (!_playerToFormation.TryGetValue(player, out var formation)) return false;
			// Check if character is available in the formation
			if (!formation.AvailableCharacters.Contains(character) || character.AvailableSlots <= 0) return false;

			// Free previous character slot if any
			if (_playerToCharacter.TryGetValue(player, out var previousCharacter)) previousCharacter.UsedSlots--;

			_playerToCharacter[player] = character;
			character.UsedSlots++;
			return true;
		}

		public void DeclareCandidacy(NetworkCommunicator player, string pitch)
		{
			if (player == null)
			{
				Log("Player or pitch is null/empty", LogLevel.Error);
				return;
			}
			if (!_playerToFormation.TryGetValue(player, out var formation))
			{
				Log($"{player.UserName} isn't part of a formation", LogLevel.Error);
				return;
			}
			if (!formation.CandidateInfo.TryGetValue(player, out CandidateInfo candidateInfo))
			{
				formation.CandidateInfo[player] = candidateInfo = new CandidateInfo(player) { Pitch = pitch, Votes = 0 };
				formation.Candidates.Add(candidateInfo);
			}
			else
			{
				candidateInfo.Pitch = pitch;
			}
		}

		public bool ToggleVote(PlayerFormation formation, NetworkCommunicator voter, CandidateInfo candidate)
		{
			if (!formation.Candidates.Contains(candidate))
			{
				Log($"{voter.UserName} is not a candidate in {formation.Name}", LogLevel.Error);
				return false;
			}
			if (!formation.PlayerVotes.TryGetValue(voter, out var playerVotes))
			{
				formation.PlayerVotes[voter] = playerVotes = new HashSet<CandidateInfo>();
			}
			if (playerVotes.Contains(candidate))
			{
				// Remove vote
				playerVotes.Remove(candidate);
				candidate.Votes--;
				return false;
			}
			// Add vote
			playerVotes.Add(candidate);
			candidate.Votes++;
			return true;
		}

		public void ElectOfficer(PlayerFormation formation)
		{
			if (formation.Candidates.Count == 0)
			{
				Log($"No candidates for {formation.Name}", LogLevel.Error);
				return;
			}

			NetworkCommunicator officer = formation.Candidates
				.OrderByDescending(c => c.Votes)
				.First().Player;

			formation.SetOfficer(officer);
		}

		public void RemovePlayerFromFormation(PlayerFormation formation, NetworkCommunicator player)
		{
			// Remove selected character
			if (_playerToCharacter.TryGetValue(player, out var selectedCharacter))
			{
				selectedCharacter.UsedSlots--;
				_playerToCharacter.Remove(player);
			}

			// Check if player is the officer
			if (formation.Officer == player)
			{
				formation.SetOfficer(null);
			}

			// Check if player has votes
			if (formation.PlayerVotes.TryGetValue(player, out var playerVotes))
			{
				foreach (CandidateInfo ci in playerVotes)
				{
					ci.Votes--;
				}
				formation.PlayerVotes.Remove(player);
			}

			// Check if player is a candidate
			if (formation.CandidateInfo.TryGetValue(player, out CandidateInfo candidateInfo))
			{
				// Try to remove candidate from formation
				formation.Candidates.Remove(candidateInfo);
				formation.CandidateInfo.Remove(player);

				// Remove that player from everyone's votes
				foreach (NetworkCommunicator formationMember in formation.Members)
				{
					if (formation.PlayerVotes.TryGetValue(formationMember, out var votes))
					{
						votes.Remove(candidateInfo);
					}
				}
			}

			// Remove player from formation
			formation.Members.Remove(player);
			_playerToFormation.Remove(player);
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

		private int GetNextTeamIndex()
		{
			int index = 0;
			while (Teams.Any(t => t.Index == index))
				index++;
			return index++;
		}

		private int GetNextFormationIndex(PlayerTeam team)
		{
			int index = 0;
			while (team.Formations.Any(f => f.Index == index))
				index++;
			return index;
		}

		private int GetNextCharacterIndex(PlayerFormation formation)
		{
			int index = 0;
			while (formation.AvailableCharacters.Any(c => c.Index == index))
				index++;
			return index;
		}
	}
}
