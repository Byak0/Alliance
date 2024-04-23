using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Client.GameModes.CaptainX
{
    public class CaptainGameMode : MissionBasedMultiplayerGameMode
    {
        public CaptainGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("CaptainX", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return GetMissionBehaviors();
            }, true, true);
        }

        private MissionBehavior[] GetMissionBehaviors()
        {
            MissionBehavior[] behaviors = new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    new FormationBehavior(),

                    new MultiplayerAchievementComponent(),
                    new MultiplayerWarmupComponent(),
                    new ALMissionMultiplayerFlagDominationClient(),
                    new MultiplayerRoundComponent(),
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new ConsoleMatchStartEndHandler(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MultiplayerTeamSelectComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new AgentVictoryLogic(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new CaptainScoreboardData()),
                    MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
                    new EquipmentControllerLeaveLogic(),
                    new MissionRecentPlayersComponent(),
                    new MultiplayerPreloadHelper()
                };

            return behaviors;
        }
    }
}
