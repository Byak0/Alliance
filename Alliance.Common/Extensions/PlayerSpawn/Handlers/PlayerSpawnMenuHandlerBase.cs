using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using System.Collections.Generic;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Handlers
{
	public abstract class PlayerSpawnMenuHandlerBase
	{
		protected class SyncContext
		{
			public PlayerSpawnMenu Menu = new PlayerSpawnMenu();
			public int CurrentSyncId;
			public bool InProgress = false;
			public int AddTeamCount = 0;
			public int AddFormationCount = 0;
			public int AddCharacterCount = 0;
			public int ReceivedMessageCount = 0;
			public int? ExpectedMessageCount = null;
		}

		protected SyncContext _syncContext = new();
		protected List<IPlayerSpawnMenuMessage> _bufferedMessages = new();

		protected virtual void HandlePlayerSpawnMenuOperation(IPlayerSpawnMenuMessage message)
		{
			if (message.SyncId != _syncContext.CurrentSyncId && message.Operation != PlayerSpawnMenuOperation.BeginMenuSync)
			{
				Log($"Alliance - Buffering {message.Operation} because SyncId invalid (expected {_syncContext.CurrentSyncId}, received {message.SyncId}", LogLevel.Debug);
				_bufferedMessages.Add(message);
				return;
			}

			if (!_syncContext.InProgress && message.Operation != PlayerSpawnMenuOperation.BeginMenuSync)
			{
				Log($"Alliance - Buffering {message.Operation} because BeginMenuSync not yet received", LogLevel.Debug);
				_bufferedMessages.Add(message);
				return;
			}

			if (message.Operation != PlayerSpawnMenuOperation.BeginMenuSync && message.Operation != PlayerSpawnMenuOperation.EndMenuSync)
			{
				_syncContext.ReceivedMessageCount++;
			}

			switch (message.Operation)
			{
				case PlayerSpawnMenuOperation.BeginMenuSync:
					BeginMenuSyncHandler(message);
					break;

				case PlayerSpawnMenuOperation.EndMenuSync:
					EndMenuSyncHandler(message);
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

		protected virtual void BeginMenuSyncHandler(IPlayerSpawnMenuMessage message)
		{
			Log($"Alliance - Received BeginMenuSync", LogLevel.Debug);
			_syncContext = new SyncContext
			{
				InProgress = true,
				CurrentSyncId = message.SyncId
			};

			foreach (IPlayerSpawnMenuMessage buffered in _bufferedMessages)
			{
				// Re-process buffered messages with the current SyncId
				if (buffered.SyncId == _syncContext.CurrentSyncId)
				{
					Log($"Alliance - Processing buffered {buffered.Operation}", LogLevel.Debug);
					HandlePlayerSpawnMenuOperation(buffered);
				}
			}
			_bufferedMessages.Clear();
		}

		protected virtual void EndMenuSyncHandler(IPlayerSpawnMenuMessage message)
		{
			Log($"Alliance - Received EndMenuSync", LogLevel.Debug);
			if (!_syncContext.InProgress)
			{
				Log("Alliance - Received EndMenuSync but no BeginMenuSync was received", LogLevel.Warning);
				return;
			}

			_syncContext.ExpectedMessageCount = message.TotalMessageCount;
			if (_syncContext.ExpectedMessageCount.HasValue && _syncContext.ReceivedMessageCount != _syncContext.ExpectedMessageCount.Value)
			{
				Log($"Mismatch: Expected {_syncContext.ExpectedMessageCount}, got {_syncContext.ReceivedMessageCount}", LogLevel.Warning);
				return;
			}

			if (_syncContext.Menu.Teams.Count == 0)
			{
				Log("Alliance - Warning: EndMenuSync received but menu is empty.", LogLevel.Warning);
			}

			PlayerSpawnMenu.Instance = _syncContext.Menu;
			Log($"Alliance - PlayerSpawnMenu synched. Teams: {_syncContext.Menu.Teams.Count}", LogLevel.Information);

			_syncContext = new SyncContext();
		}

		protected virtual void AddTeamHandler(IPlayerSpawnMenuMessage message)
		{
			_syncContext.Menu?.Teams.Add(message.PlayerTeam);
			Log($"Alliance - Added team {message.PlayerTeam?.Name}", LogLevel.Debug);
		}

		protected virtual void AddFormationHandler(IPlayerSpawnMenuMessage message)
		{
			PlayerTeam team = _syncContext.Menu?.Teams.Find(t => t.Index == message.TeamIndex);
			if (team != null)
			{
				team.Formations.Add(message.PlayerFormation);
				Log($"Alliance - Added formation {message.PlayerFormation?.Name} to team {team.Name}", LogLevel.Debug);
			}
		}

		protected virtual void AddCharacterHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.AvailableCharacter == null)
			{
				Log("Alliance - Received AddCharacter with null AvailableCharacter", LogLevel.Warning);
				return;
			}
			PlayerTeam team = _syncContext.Menu.Teams.Find(t => t.Index == message.TeamIndex);
			if (team != null)
			{
				PlayerFormation formation = team.Formations.Find(f => f.Index == message.FormationIndex);
				if (formation != null)
				{
					formation.AvailableCharacters.Add(message.AvailableCharacter);
					Log($"Alliance - Added character {message.AvailableCharacter.Name} to formation {formation.Name}", LogLevel.Debug);
				}
			}
		}

		protected virtual void RemoveTeamHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.PlayerTeam == null)
			{
				Log($"Alliance - Received RemoveTeam with null PlayerTeam", LogLevel.Warning);
				return;
			}
			PlayerSpawnMenu.Instance.RemoveTeam(message.PlayerTeam);
			Log($"Alliance - Removed team at index {message.PlayerTeam.Index}", LogLevel.Debug);
		}

		protected virtual void RemoveFormationHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.PlayerFormation == null)
			{
				Log($"Alliance - Received RemoveFormation with null PlayerFormation", LogLevel.Warning);
				return;
			}
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			team.RemoveFormation(message.PlayerFormation);
			Log($"Alliance - Removed formation {message.PlayerFormation.Name} from team {team?.Name}", LogLevel.Debug);
		}

		protected virtual void RemoveCharacterHandler(IPlayerSpawnMenuMessage message)
		{
			if (message.AvailableCharacter == null)
			{
				Log($"Alliance - Received RemoveCharacter with null AvailableCharacter", LogLevel.Warning);
				return;
			}
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			if (team != null)
			{
				PlayerFormation formation = team.Formations.Find(f => f.Index == message.FormationIndex);
				if (formation != null)
				{
					formation.AvailableCharacters.Remove(message.AvailableCharacter);
					Log($"Alliance - Removed character {message.AvailableCharacter.Name} from formation {formation.Name}", LogLevel.Debug);
				}
			}
		}
	}
}
