using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Scenarios;
using Alliance.Server.Core;
using TaleWorlds.Core;

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
    }
}
