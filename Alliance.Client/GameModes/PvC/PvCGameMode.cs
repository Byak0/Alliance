using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Behaviors;
using Alliance.Common.GameModes.PvC.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Client.GameModes.PvC
{
    public class PvCGameMode : MissionBasedMultiplayerGameMode
    {
        public PvCGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("PvC", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return GetMissionBehaviors();
            }, true, true);
        }

        private MissionBehavior[] GetMissionBehaviors()
        {
            MissionBehavior[] behaviors = new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),
                    
                    // Custom components
                    new PvCGameModeClientBehavior(),
                    new MissionScoreboardComponent(new PvCScoreboardData()),
                    new PvCTeamSelectBehavior(),
                    new FormationBehavior(),

                    // Native components from Captain mode
                    new MultiplayerAchievementComponent(),
                    new MultiplayerRoundComponent(),
                    new AgentVictoryLogic(),
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent()
                };

            return behaviors;
        }
    }
}
