using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Server.Extensions.AIBehavior.TacticComponents;
using Alliance.Server.Extensions.AIBehavior.TeamAIComponents;
using Alliance.Server.Extensions.AIBehavior.Utils;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.AIBehavior.Behaviors
{
	/// <summary>
	/// Centralized behavior for handling AI-related behavior (Team AI / Tactic options...)
	/// to prevent conflicts.
	/// </summary>
	public class ALGlobalAIBehavior : MissionNetwork
	{
		private MultiplayerRoundController _roundController;

		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

		public override void OnBehaviorInitialize()
		{
			_roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
		}

		public override void AfterStart()
		{
			if (_roundController != null)
			{
				_roundController.OnRoundStarted += OnRoundStart;
			}
		}

		public override void OnRemoveBehavior()
		{
			if (_roundController != null)
			{
				_roundController.OnRoundStarted -= OnRoundStart;
			}
		}

		private void OnRoundStart()
		{
			EnableAIAfterTimer(Config.Instance.TimeBeforeStart * 1000 + MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
		}

		private async void EnableAIAfterTimer(int waitTime)
		{
			await Task.Delay(waitTime);
			AddTeamAI(Mission.Current.AttackerTeam);
			AddTeamAI(Mission.Current.DefenderTeam);
		}

		private void AddTeamAI(Team team)
		{
			// Find or create TeamAI
			TeamAIComponent teamAI;
			string gameType = MultiplayerOptions.OptionType.GameType.GetStrValue();
			if (gameType == "CvC" || gameType == "PvC" || gameType == "BattleX" || gameType == "CaptainX")
			{
				teamAI = new ALTeamAIGeneral(Mission.Current, team);
				teamAI.AddTacticOption(new ALFlagTacticComponent(team));
			}
			else
			{
				teamAI = new ALTeamAIGeneralSoft(Mission.Current, team);
				teamAI.AddTacticOption(new SimpleTacticComponent(team));
			}
			teamAI.OnTacticAppliedForFirstTime();
			TeamQuerySystemUtils.SetPowerFix(Mission.Current);
			foreach (Formation formation in team.FormationsIncludingSpecialAndEmpty)
			{
				teamAI.OnUnitAddedToFormationForTheFirstTime(formation);
			}
			team.AddTeamAI(teamAI);
			bool playerIsControlling = !FormationControlModel.Instance.GetAllControllersFromTeam(team).IsEmpty();
			team.SetPlayerRole(playerIsControlling, playerIsControlling);
			Log($"Team AI added for {team.Side}", LogLevel.Debug);
		}
	}
}
