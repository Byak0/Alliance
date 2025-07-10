using Alliance.Common.Extensions;
using Alliance.Common.Extensions.PlayerSpawn.Handlers;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
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
			reg.Register<SyncPlayerCharacterUsage>(HandlePlayerCharacterUsage);
			reg.Register<SyncFormationMainLanguage>(HandleFormationMainLanguage);
		}

		private void HandlePlayerCharacterUsage(SyncPlayerCharacterUsage message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			AvailableCharacter character = formation?.AvailableCharacters.Find(c => c.Index == message.CharacterIndex);
			if (message.Player == null || team == null || formation == null || character == null)
			{
				Log($"Alliance - PlayerSpawnMenu - {message.Player} requested invalid character usage: Team {message.TeamIndex}, Formation {message.FormationIndex}, Character {message.CharacterIndex}", LogLevel.Warning);
				return;
			}

			PlayerSpawnMenu.Instance.SelectCharacter(message.Player, team, formation, character);
		}

		private void HandleFormationMainLanguage(SyncFormationMainLanguage message)
		{
			PlayerTeam team = PlayerSpawnMenu.Instance.Teams.Find(t => t.Index == message.TeamIndex);
			PlayerFormation formation = team?.Formations.Find(f => f.Index == message.FormationIndex);
			string mainLanguage = LocalizationHelper.GetLanguage(message.MainLanguageIndex);
			if (team == null || formation == null)
			{
				Log($"Alliance - PlayerSpawnMenu - Update Language - Team of formation not found: Team {message.TeamIndex}, Formation {message.FormationIndex}", LogLevel.Warning);
				return;
			}
			formation.MainLanguage = mainLanguage;
		}
	}
}
