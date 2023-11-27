using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.BattleX
{
    public class BattleGameMode : MissionBasedMultiplayerGameMode
    {
        public BattleGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("BattleX", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new FormationBehavior(),

                    new MultiplayerRoundController(),
                    //new MissionMultiplayerFlagDomination(MissionLobbyComponent.MultiplayerGameType.Battle),
                    new PvCMissionMultiplayerFlagDomination(MultiplayerGameType.Battle),
                    //new MissionMultiplayerGameModeFlagDominationClient(),
                    new PvCMissionMultiplayerGameModeFlagDominationClient(),
                    new MultiplayerWarmupComponent(),
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new ConsoleMatchStartEndHandler(),
                    new SpawnComponent(new PvCFlagDominationSpawnFrameBehavior(), new PvCFlagDominationSpawningBehavior()),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MultiplayerTeamSelectComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new AgentVictoryLogic(),
                    new AgentHumanAILogic(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerAdminComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new BattleScoreboardData()),
                    new EquipmentControllerLeaveLogic(),
                    new MultiplayerPreloadHelper()
                };
            }, true, true);
        }
    }
}
