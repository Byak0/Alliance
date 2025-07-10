using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.Core;
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

		public PlayerAssignment(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation, List<int> perks)
		{
			Player = player;
			Team = team;
			Formation = formation;
			Perks = perks;
		}
	}

	[Serializable]
	public class PlayerSpawnMenu
	{
		// XML-serializable properties
		public List<PlayerTeam> Teams { get; set; } = new();

		// Lookup properties
		[XmlIgnore]
		private readonly Dictionary<NetworkCommunicator, PlayerAssignment> _playerAssignments = new();
		[XmlIgnore]
		public PlayerAssignment MyAssignment => GetPlayerAssignment(GameNetwork.MyPeer);

		// Events for UI updates
		public event Action<NetworkCommunicator, int, int, int> OnCharacterSelected;
		public event Action<NetworkCommunicator, int, int, int> OnCharacterDeselected;
		public event Action<string, int, int> OnFormationLanguageUpdated;

		public static PlayerSpawnMenu Instance { get; set; } = new PlayerSpawnMenu();

		public PlayerAssignment GetPlayerAssignment(NetworkCommunicator player)
		{
			if (player == null) return null;
			if (_playerAssignments.TryGetValue(player, out var assignment))
			{
				return assignment;
			}
			_playerAssignments[player] = new PlayerAssignment(player, null, null, new List<int>());
			return _playerAssignments[player];
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

		public void SelectTeam(PlayerTeam team)
		{
			// Default to native team if no team is provided
			if (team == null)
			{
				BattleSideEnum mySide = GameNetwork.MyPeer?.GetComponent<MissionPeer>()?.Team?.Side ?? BattleSideEnum.Defender;
				MyAssignment.Team = Teams.FirstOrDefault(t => t.TeamSide == mySide);
			}
			else
			{
				MyAssignment.Team = team;
			}
		}

		private void MovePlayerToFormation(NetworkCommunicator player, PlayerTeam team, PlayerFormation formation)
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
					SyncFormationLanguage(team, formation, formation.MainLanguage);
				}
			}
		}

		public void SyncFormationLanguage(PlayerTeam team, PlayerFormation formation, string language)
		{
			int mainLanguageIndex = LocalizationHelper.GetAvailableLanguages().IndexOf(language);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncFormationMainLanguage(team, formation, mainLanguageIndex));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public void RequestToUseCharacter(PlayerTeam team, PlayerFormation formation, AvailableCharacter character, List<int> selectedPerks)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestCharacterUsage(team, formation, character, selectedPerks));
			GameNetwork.EndModuleEventAsClient();
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
			string perks = "";
			selectedPerks.ForEach(i => perks += i);
			Log($"{assignment.Player.UserName} updated perks for {assignment.Character.Name} : {perks}");
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
			if (GameNetwork.IsServer) ClearDisconnectedPlayers();

			PlayerAssignment assignment = GetPlayerAssignment(player);
			// Free previous character slot if any
			if (assignment.Character != null)
			{
				assignment.Character.UsedSlots--;
				OnCharacterDeselected?.Invoke(player, assignment.Team.Index, assignment.Formation.Index, assignment.Character.Index);
			}

			// Add player to formation if not already
			if (!formation.Members.Contains(player))
			{
				MovePlayerToFormation(player, team, formation);
			}
			assignment.Character = character;
			character.UsedSlots++;

			OnCharacterSelected?.Invoke(player, team.Index, formation.Index, character.Index);
		}

		// todo : find a way to clear disconnected players on client side too (send a message from server ?)
		private void ClearDisconnectedPlayers()
		{
			List<NetworkCommunicator> toRemove = new();

			foreach (KeyValuePair<NetworkCommunicator, PlayerAssignment> kvp in _playerAssignments)
			{
				if (!kvp.Key.IsConnectionActive) // Seems to be always true on client side so only check on server side
				{
					toRemove.Add(kvp.Key);
				}
			}

			foreach (NetworkCommunicator key in toRemove)
			{
				PlayerAssignment assignment = _playerAssignments[key];
				if (assignment.Character != null)
				{
					assignment.Character.UsedSlots--;
					OnCharacterDeselected?.Invoke(key, assignment.Team.Index, assignment.Formation.Index, assignment.Character.Index);
				}

				_playerAssignments.Remove(key);
				Log($"Removed {key.UserName} as he is no longer connected", LogLevel.Debug);
			}
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
	}
}
