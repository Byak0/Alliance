using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Behaviors;
using Alliance.Common.GameModes.PvC.Models;
using Alliance.Server.Extensions.GameModeMenu.Behaviors;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.Extensions.SimpleRespawn.Behaviors;
using Alliance.Server.GameModes.PvC.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.PvC
{
    public class PvCGameMode : MissionBasedMultiplayerGameMode
    {
        public PvCGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("PvC", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                Debug.Print("Loading of all Behaviors :", 0, Debug.DebugColor.Blue);
                List<MissionBehavior> allBehaviors = GetCustomMissionBehaviors();
                allBehaviors.AddRange(GetNativeMissionBehaviors());
                return allBehaviors;
            }, true, true);
        }

        private List<MissionBehavior> GetCustomMissionBehaviors()
        {
            List<MissionBehavior> behaviors = new List<MissionBehavior>
            {
                    MissionLobbyComponent.CreateBehavior(),

                    // Custom components
                    new SpawnComponent(new PvCSpawnFrameBehavior(), new PvCSpawningBehavior()),
                    new PvCGameModeBehavior(MultiplayerGameType.Captain),
                    new PvCGameModeClientBehavior(),
                    new MissionScoreboardComponent(new PvCScoreboardData()),
                    new PvCTeamSelectBehavior(),
                    new RespawnBehavior(),
                    new PollBehavior(),
                    new FormationBehavior()
            };

            if (Config.Instance.ActivateSAE)
            {
                behaviors.Add(new SaeBehavior());
            }

            return behaviors;
        }

        private List<MissionBehavior> GetNativeMissionBehaviors()
        {

            return new List<MissionBehavior>()
            {
                    // Native behaviors
                    new MultiplayerRoundController(),
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
        }
    }
}
