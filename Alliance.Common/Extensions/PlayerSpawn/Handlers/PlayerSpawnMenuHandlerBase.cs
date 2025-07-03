using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMessage;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Handlers
{
	public abstract class PlayerSpawnMenuHandlerBase
	{
		protected PlayerSpawnMenu _receivedPlayerSpawnMenu = new PlayerSpawnMenu();
		protected bool _syncInProgress = false;

		protected virtual void HandlePlayerSpawnMenuOperation(PlayerSpawnMenuMessage message)
		{
			switch (message.Operation)
			{
				case PlayerSpawnMenuOperation.BeginMenuSync:
					BeginMenuSyncHandler();
					break;

				case PlayerSpawnMenuOperation.EndMenuSync:
					EndMenuSyncHandler();
					break;

				case PlayerSpawnMenuOperation.AddTeam:
					AddTeamHandler(message);
					break;

				case PlayerSpawnMenuOperation.AddFormation:
					AddFormationHandler(message);
					break;

				case PlayerSpawnMenuOperation.AddCharacter:
					AddCharacterHandler(message);
					break;

				case PlayerSpawnMenuOperation.RemoveTeam:
					RemoveTeamHandler(message);
					break;

				case PlayerSpawnMenuOperation.RemoveFormation:
					RemoveFormationHandler(message);
					break;

				case PlayerSpawnMenuOperation.RemoveCharacter:
					RemoveCharacterHandler(message);
					break;

				default:
					Log($"Alliance - Unknown operation {message.Operation}", LogLevel.Warning);
					break;
			}
		}

		protected virtual void BeginMenuSyncHandler()
		{
			Log($"Alliance - Received BeginMenuSync", LogLevel.Debug);
			_receivedPlayerSpawnMenu = new PlayerSpawnMenu();
			_syncInProgress = true;
		}

		protected virtual void EndMenuSyncHandler()
		{
			Log($"Alliance - Received EndMenuSync", LogLevel.Debug);
			if (!_syncInProgress)
			{
				Log("Alliance - Received EndMenuSync but no BeginMenuSync was received", LogLevel.Warning);
				return;
			}
			PlayerSpawnMenu.Instance = _receivedPlayerSpawnMenu;
			_syncInProgress = false;
			Log($"Alliance - PlayerSpawn menu initialized with {_receivedPlayerSpawnMenu.Teams?.Count} teams", LogLevel.Information);
		}

		protected virtual void AddTeamHandler(PlayerSpawnMenuMessage message)
		{
			_receivedPlayerSpawnMenu?.Teams.Add(message.PlayerTeam);
			Log($"Alliance - Added team {message.PlayerTeam?.Name}", LogLevel.Debug);
		}

		protected virtual void AddFormationHandler(PlayerSpawnMenuMessage message)
		{
			PlayerTeam team = _receivedPlayerSpawnMenu?.Teams.Find(t => t.Index == message.TeamIndex);
			if (team != null)
			{
				team.Formations.Add(message.PlayerFormation);
				Log($"Alliance - Added formation {message.PlayerFormation?.Name} to team {team.Name}", LogLevel.Debug);
			}
		}

		protected virtual void AddCharacterHandler(PlayerSpawnMenuMessage message)
		{
			if (message.AvailableCharacter == null)
			{
				Log("Alliance - Received AddCharacter with null AvailableCharacter", LogLevel.Warning);
				return;
			}
			PlayerTeam team2 = _receivedPlayerSpawnMenu.Teams.Find(t => t.Index == message.TeamIndex);
			if (team2 != null)
			{
				PlayerFormation formation = team2.Formations.Find(f => f.Index == message.FormationIndex);
				if (formation != null)
				{
					formation.AvailableCharacters.Add(message.AvailableCharacter);
					Log($"Alliance - Added character {message.AvailableCharacter.Name} to formation {formation.Name}", LogLevel.Debug);
				}
			}
		}

		protected virtual void RemoveTeamHandler(PlayerSpawnMenuMessage message)
		{
			if (message.PlayerTeam == null)
			{
				Log($"Alliance - Received RemoveTeam with null PlayerTeam", LogLevel.Warning);
				return;
			}
			PlayerSpawnMenu.Instance.RemoveTeam(message.PlayerTeam);
			Log($"Alliance - Removed team at index {message.PlayerTeam.Index}", LogLevel.Debug);
		}

		protected virtual void RemoveFormationHandler(PlayerSpawnMenuMessage message)
		{
			if (message.PlayerFormation == null)
			{
				Log($"Alliance - Received RemoveFormation with null PlayerFormation", LogLevel.Warning);
				return;
			}
			PlayerTeam team3 = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerSpawnMenu.Instance.RemoveFormation(team3, message.PlayerFormation);
			Log($"Alliance - Removed formation {message.PlayerFormation.Name} from team {team3?.Name}", LogLevel.Debug);
		}

		protected virtual void RemoveCharacterHandler(PlayerSpawnMenuMessage message)
		{
			if (message.AvailableCharacter == null)
			{
				Log($"Alliance - Received RemoveCharacter with null AvailableCharacter", LogLevel.Warning);
				return;
			}
			PlayerTeam team4 = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			if (team4 != null)
			{
				PlayerFormation formation = team4.Formations.Find(f => f.Index == message.FormationIndex);
				if (formation != null)
				{
					formation.AvailableCharacters.Remove(message.AvailableCharacter);
					Log($"Alliance - Removed character {message.AvailableCharacter.Name} from formation {formation.Name}", LogLevel.Debug);
				}
			}
		}
	}
}
