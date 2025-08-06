using Alliance.Common.Extensions;
using Alliance.Common.Extensions.PlayerSpawn.Handlers;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.PlayerSpawn.Handlers
{
	public class PlayerSpawnClientHandler : PlayerSpawnMenuHandlerBase, IHandlerRegister
	{
		private const float RETRY_COOLDOWN = 5.0f;
		private DateTime _lastRetryTime = DateTime.MinValue;

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncPlayerSpawnMenu>(HandlePlayerSpawnMenuOperation);
			reg.Register<AddCharacterUsage>(HandleAddCharacterUsage);
			reg.Register<RemoveCharacterUsage>(HandleRemoveCharacterUsage);
			reg.Register<SyncFormationMainLanguage>(HandleFormationMainLanguage);
			reg.Register<AddOfficerCandidacy>(HandleAddOfficerCandidacy);
			reg.Register<RemoveOfficerCandidacy>(HandleRemoveOfficerCandidacy);
			reg.Register<SetFormationOfficer>(HandleSetFormationOfficer);
			reg.Register<SetElectionStatus>(HandleSetElectionTimer);
			reg.Register<SetSpawnStatus>(HandleSetSpawnStatus);
			reg.Register<RemovePlayerFromFormation>(HandleRemovePlayerFromFormation);
			reg.Register<SetPlayerTeam>(HandleSetPlayerTeam);
		}

		protected override void EndMenuSyncHandler(IPlayerSpawnMenuMessage message)
		{
			_syncContext.ExpectedMessageCount = message.TotalMessageCount;
			if (_syncContext.ExpectedMessageCount.HasValue && _syncContext.ReceivedMessageCount != _syncContext.ExpectedMessageCount.Value)
			{
				Log($"Mismatch: Expected {_syncContext.ExpectedMessageCount}, got {_syncContext.ReceivedMessageCount}", LogLevel.Warning);
				// Request server to send the menu again if last retry was old enough
				if ((DateTime.Now - _lastRetryTime).TotalSeconds > RETRY_COOLDOWN)
				{
					_lastRetryTime = DateTime.Now;
					PlayerSpawnMenuMsg.RequestPlayerSpawnMenu();
					Log("Alliance - Requesting player spawn menu again due to mismatch", LogLevel.Warning);
				}
				else
				{
					Log("Alliance - Skipping retry, cooldown not met", LogLevel.Warning);
				}
			}

			base.EndMenuSyncHandler(message);
		}

		private void HandleAddCharacterUsage(AddCharacterUsage message)
		{
			if (PlayerSpawnMenu.Instance?.Teams == null) return;

			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			AvailableCharacter character = formation?.AvailableCharacters.Find(c => c.Index == message.CharacterIndex);
			if (message.Player == null || team == null || formation == null || character == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} requested invalid character usage: Team {message.TeamIndex}, Formation {message.FormationIndex}, Character {message.CharacterIndex}", LogLevel.Error);
				return;
			}

			PlayerSpawnMenu.Instance.SelectCharacter(message.Player, team, formation, character);
		}

		private void HandleRemoveCharacterUsage(RemoveCharacterUsage message)
		{
			if (message.Player == null)
			{
				Log($"Alliance - PlayerSpawnMenu - Can't clear character usage, player is null", LogLevel.Error);
				return;
			}

			PlayerSpawnMenu.Instance.ClearCharacterSelection(message.Player);
		}

		private void HandleSetFormationOfficer(SetFormationOfficer message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);

			if (team == null || formation == null)
			{
				Log($"Alliance - PlayerSpawnMenu - Set officer - Invalid team/formation : Team {message.TeamIndex}, Formation {message.FormationIndex}", LogLevel.Error);
				return;
			}

			formation.SetOfficer(message.NewOfficer);
		}

		private void HandleAddOfficerCandidacy(AddOfficerCandidacy message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			AvailableCharacter character = formation?.AvailableCharacters.Find(c => c.Index == message.CharacterIndex);

			if (message.Player == null || team == null || formation == null || character == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} requested invalid character usage: Team {message.TeamIndex}, Formation {message.FormationIndex}, Character {message.CharacterIndex}", LogLevel.Error);
				return;
			}

			if (formation.CandidateInfo.ContainsKey(message.Player))
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} is already a candidate in formation {formation.Name}", LogLevel.Error);
				return;
			}

			// Add candidate to the formation
			formation.AddCandidate(message.Player, message.Pitch);
		}

		private void HandleRemoveOfficerCandidacy(RemoveOfficerCandidacy message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);

			if (message.Player == null || team == null || formation == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} candidacy can't be removed from: Team {message.TeamIndex}, Formation {message.FormationIndex}", LogLevel.Error);
				return;
			}

			if (!formation.CandidateInfo.TryGetValue(message.Player, out CandidateInfo candidateInfo))
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} is not a candidate in formation {formation.Name}", LogLevel.Error);
				return;
			}

			formation.RemoveCandidate(candidateInfo);
		}

		private void HandleFormationMainLanguage(SyncFormationMainLanguage message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			string mainLanguage = LocalizationHelper.GetLanguage(message.MainLanguageIndex);
			if (team == null || formation == null)
			{
				Log($"Alliance - PlayerSpawnMenu - Update Language - Team of formation not found: Team {message.TeamIndex}, Formation {message.FormationIndex}", LogLevel.Error);
				return;
			}
			formation.MainLanguage = mainLanguage;
		}

		private void HandleSetElectionTimer(SetElectionStatus message)
		{
			if (message.Enable)
			{
				PlayerSpawnMenu.Instance.StartOfficerElection(message.Timer);
			}
			else
			{
				PlayerSpawnMenu.Instance.EndElection();
			}

		}

		private void HandleSetSpawnStatus(SetSpawnStatus message)
		{
			if (message.Enable)
			{
				PlayerSpawnMenu.Instance.StartSpawnTimer(message.Timer);
			}
			else
			{
				PlayerSpawnMenu.Instance.EndSpawn();
			}
		}

		private void HandleRemovePlayerFromFormation(RemovePlayerFromFormation message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);

			if (message.Player == null || team == null || formation == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} can't be removed from: Team {message.TeamIndex}, Formation {message.FormationIndex}", LogLevel.Error);
				return;
			}

			if (!formation.Members.Contains(message.Player))
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} is not a member of formation {formation.Name}", LogLevel.Error);
				return;
			}

			PlayerSpawnMenu.Instance.RemoveFormationMember(team, formation, message.Player);
		}

		private void HandleSetPlayerTeam(SetPlayerTeam message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerSpawnMenu.Instance.SetPlayerTeam(message.Player, team);
		}
	}
}
