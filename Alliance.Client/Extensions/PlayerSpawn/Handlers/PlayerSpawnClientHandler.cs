using Alliance.Common.Extensions;
using Alliance.Common.Extensions.PlayerSpawn.Handlers;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using Alliance.Common.Extensions.PlayerSpawn.Views;
using Alliance.Common.GameModes.Story.Utilities;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.PlayerSpawn.Handlers
{
	public class PlayerSpawnClientHandler : PlayerSpawnMenuHandlerBase, IHandlerRegister
	{
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
		}

		protected override void EndMenuSyncHandler()
		{
			base.EndMenuSyncHandler();

			// Try to select a default team after a complete sync
			PlayerSpawnMenu.Instance.SelectTeam();

			// Force reopening the menu to refresh it completely
			PlayerSpawnMenuView view = Mission.Current?.GetMissionBehavior<PlayerSpawnMenuView>();
			if (view != null && view.IsMenuOpen)
			{
				view.CloseMenu();
				view.OpenMenu(PlayerSpawnMenu.Instance);
			}
		}

		private void HandleAddCharacterUsage(AddCharacterUsage message)
		{
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
			AvailableCharacter character = formation?.AvailableCharacters.Find(c => c.Index == message.CharacterIndex);

			if (message.Player == null || team == null || formation == null || character == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} requested invalid character usage: Team {message.TeamIndex}, Formation {message.FormationIndex}, Character {message.CharacterIndex}", LogLevel.Error);
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
	}
}
