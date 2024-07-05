using Alliance.Client.GameModes.Story.Views;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Scenarios;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.GameModes.Story.Scenarios
{
	public class ClientScenarios
	{
		public static Scenario BFHD()
		{
			return CommonScenarios.BFHD(
					onActShowResults: winnerSide =>
					{
						BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
						ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
						string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
						string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
						string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
						Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
						sv.ShowResultScreen(
								GameTexts.FindText($"str_alliance_act_{suffix}", "BFHD_1").ToString(),
								GameTexts.FindText($"str_alliance_act_{suffix}", "BFHD_1").ToString(),
								color);
					},
					onActCompleted: null,
					onActShowResults2: winnerSide =>
					{
						BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
						ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
						string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
						string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
						string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
						Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
						sv.ShowResultScreen(
								GameTexts.FindText($"str_alliance_act_{suffix}", "BFHD_2").ToString(),
								GameTexts.FindText($"str_alliance_act_{suffix}", "BFHD_2").ToString(),
								color);
					},
					onActCompleted2: null);
		}

		public static Scenario TestGiant()
		{
			return CommonScenarios.TestGiant(
					onActShowResults: winnerSide =>
					{
						BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
						ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
						string text = winnerSide == playerSide ? "You win" : "You lose";
						Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
						sv.ShowResultScreen(
								text,
								text,
								color);
					},
					onActCompleted: null);
		}

		public static Scenario GP()
		{
			return CommonScenarios.GP(
				onActShowResults: winnerSide =>
				{
					BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
					ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
					string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
					string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
					string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
					Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
					sv.ShowResultScreen(
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_1").ToString(),
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_1").ToString(),
							color);
				},
				onActCompleted: null,
				onActShowResults2: winnerSide =>
				{
					BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
					ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
					string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
					string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
					string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
					Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
					sv.ShowResultScreen(
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_2").ToString(),
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_2").ToString(),
							color);
				},
				onActCompleted2: null,
				onActShowResults3: winnerSide =>
				{
					BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
					ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
					string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
					string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
					string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
					Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
					sv.ShowResultScreen(
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_3").ToString(),
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_3").ToString(),
							color);
				},
				onActCompleted3: null,
				onActShowResults4: winnerSide =>
				{
					BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
					ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
					string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
					string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
					string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
					Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
					sv.ShowResultScreen(
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_4").ToString(),
							GameTexts.FindText($"str_alliance_act_{suffix}", "GP_4").ToString(),
							color);
				},
				onActCompleted4: null);
		}

		public static Scenario GdCFinal()
		{
			return CommonScenarios.GdCFinal(
				onActShowResults: winnerSide =>
				{
					if (Mission.Current.PlayerTeam == null)
					{
						return;
					}
					BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
					ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
					string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
					string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
					string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
					Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
					sv.ShowResultScreen(
							GameTexts.FindText($"str_alliance_act_{suffix}", "GdCFinal_1").ToString(),
							GameTexts.FindText($"str_alliance_act_{suffix}", "GdCFinal_1").ToString(),
							color);
				},
				onActCompleted: null);
		}

		public static Scenario OrgaDefault()
		{
			return CommonScenarios.OrgaDefault(
				onActShowResults: winnerSide =>
				{
					if (Mission.Current.PlayerTeam == null)
					{
						return;
					}
					BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
					ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
					string winnerSuffix = winnerSide == BattleSideEnum.Attacker ? "victory_attacker" : "victory_defender";
					string loserSuffix = winnerSide == BattleSideEnum.Attacker ? "lose_defender" : "lose_attacker";
					string suffix = winnerSide == playerSide ? winnerSuffix : loserSuffix;
					Color color = winnerSide == playerSide ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
					sv.ShowResultScreen(
							GameTexts.FindText($"str_alliance_act_{suffix}", "OrgaDefault_1").ToString(),
							GameTexts.FindText($"str_alliance_act_{suffix}", "OrgaDefault_1").ToString(),
							color);
				},
				onActCompleted: null);
		}
	}
}
