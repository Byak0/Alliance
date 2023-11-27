using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Behaviors;
using Alliance.Common.GameModes.PvC.Models;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Server.Extensions.FlagsTracker.Behaviors;
using Alliance.Server.Extensions.GameModeMenu.Behaviors;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.GameModes.Story.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.Story
{
    public class ScenarioGameMode : MissionBasedMultiplayerGameMode
    {
        public ScenarioGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("Scenario", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return getMissionBehaviors();
            }, true, true);
        }

        private List<MissionBehavior> getMissionBehaviors()
        {
            List<MissionBehavior> behaviors = new List<MissionBehavior>()
            {
                    MissionLobbyComponent.CreateBehavior(),

                    // Custom components
                    new SpawnComponent(new ScenarioDefaultSpawnFrameBehavior(), new ScenarioSpawningBehavior()),
                    new ScenarioBehavior(),
                    new ScenarioClientBehavior(),
                    new MissionScoreboardComponent(new PvCScoreboardData()), // todo : replace with custom objectives ui
                    new PvCTeamSelectBehavior(),
                    new ScenarioRespawnBehavior(),
                    new PollBehavior(),
                    new FormationBehavior(),
                    new ObjectivesBehavior(ScenarioManagerServer.Instance),
                    new FlagTrackerBehavior(),
                    new CapturableZoneBehavior(),

                    // Native components
                    //new MultiplayerRoundController(), // todo : remove (replace with scenario system)
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new AgentHumanAILogic(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent()
            };

            if (Config.Instance.ActivateSAE)
            {
                behaviors.Add(new SaeBehavior());
            }

            return behaviors;
        }
    }
}
