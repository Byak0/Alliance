using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.Extensions.VOIP.Behaviors;
using Alliance.Common.GameModes.Captain.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Client.GameModes.BattleX
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
                    new FormationBehavior(),
                    new VoipHandler(),

                    MissionLobbyComponent.CreateBehavior(),
                    new MultiplayerRoundComponent(),
                    new MultiplayerWarmupComponent(),
                    new ALMissionMultiplayerFlagDominationClient(),
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new ConsoleMatchStartEndHandler(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MultiplayerTeamSelectComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerAdminComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new BattleScoreboardData()),
                    MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
                    new EquipmentControllerLeaveLogic(),
                    new MultiplayerPreloadHelper()
                };
            }, true, true);
        }
    }
}
