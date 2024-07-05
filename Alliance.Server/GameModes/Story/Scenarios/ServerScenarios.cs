using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Scenarios;
using Alliance.Server.Core;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Scenarios
{
	// TODO : Update with correct factions / maps.
	public class ServerScenarios
	{
		public static Scenario BFHD()
		{
			return CommonScenarios.BFHD(
					onActShowResults: null,
					onActCompleted: side =>
					{
						if (side == BattleSideEnum.Attacker) GameModeStarter.Instance.StartLobby("bfhd_helms_deep_lobby", "battania", "vlandia");
						else ScenarioManagerServer.Instance.StartScenario("BFHD", 1);
					},
					onActShowResults2: null,
					onActCompleted2: side =>
					{
						GameModeStarter.Instance.StartLobby("bfhd_helms_deep_lobby", "battania", "vlandia");
					});
		}

		public static Scenario TestGiant()
		{
			return CommonScenarios.TestGiant(
					onActShowResults: null,
					onActCompleted: side =>
					{
						GameModeStarter.Instance.StartLobby("bfhd_helms_deep_lobby", "battania", "vlandia");
					});
		}

		public static Scenario GP()
		{
			return CommonScenarios.GP(
					onActShowResults: null,
					onActCompleted: side =>
					{
						if (side == BattleSideEnum.Attacker) ScenarioManagerServer.Instance.StartScenario("GP", 1);
						else GameModeStarter.Instance.StartLobby("FrenchCastlePathEdC", "empire", "battania");
					},
					onActShowResults2: null,
					onActCompleted2: side =>
					{
						if (side == BattleSideEnum.Attacker) ScenarioManagerServer.Instance.StartScenario("GP", 2);
						else GameModeStarter.Instance.StartLobby("FrenchCastlePathEdC", "empire", "battania");
					},
					onActShowResults3: null,
					onActCompleted3: side =>
					{
						if (side == BattleSideEnum.Attacker) ScenarioManagerServer.Instance.StartScenario("GP", 3);
						else GameModeStarter.Instance.StartLobby("FrenchCastlePathEdC", "empire", "battania");
					},
					onActShowResults4: null,
					onActCompleted4: side =>
					{
						GameModeStarter.Instance.StartLobby("FrenchCastlePathEdC", "empire", "battania");
					});
		}

		public static Scenario GdCFinal()
		{
			return CommonScenarios.GdCFinal(
					onActShowResults: null,
					onActCompleted: side =>
					{
						if (side == BattleSideEnum.Attacker) GameModeStarter.Instance.StartLobby("bfhd_helms_deep_v2", "isengard", "rohan");
						else GameModeStarter.Instance.StartLobby("bfhd_helms_deep_v2", "rohan", "isengard");
					});
		}

		public static Scenario OrgaDefault()
		{
			string map = MultiplayerOptions.OptionType.Map.GetStrValue();
			string attacker = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
			string defender = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
			return CommonScenarios.OrgaDefault(
					onActShowResults: null,
					onActCompleted: side =>
					{
						if (side == BattleSideEnum.Attacker) GameModeStarter.Instance.StartLobby(map, attacker, defender);
						else GameModeStarter.Instance.StartLobby(map, defender, attacker);
					});
		}
	}
}
