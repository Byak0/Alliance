using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.Extensions.VOIP.Behaviors;
using Alliance.Common.GameModes.CvC.Behaviors;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.Extensions.SimpleRespawn.Behaviors;
using Alliance.Server.Extensions.TeamKillTracker.Behavior;
using Alliance.Server.GameModes.PvC.Behaviors;
using Alliance.Server.Patch.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.CvC
{
    public class CvCGameMode : MissionBasedMultiplayerGameMode
    {
        public CvCGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("CvC", new MissionInitializerRecord(scene), (missionController) => GetMissionBehaviors(), true, true);
        }

        private List<MissionBehavior> GetMissionBehaviors()
        {
            List<MissionBehavior> behaviors = new List<MissionBehavior>
            {
                    new AllianceLobbyComponent(),
                    new FormationBehavior(),
                    new VoipHandler(),
                    new RespawnBehavior(),
                    new PvCGameModeBehavior(MultiplayerGameType.Captain),
                    new MultiplayerRoundController(),
                    new CvCGameModeClientBehavior(),
                    new MultiplayerTimerComponent(),
                    new SpawnComponent(new PvCSpawnFrameBehavior(), new PvCSpawningBehavior()),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MultiplayerTeamSelectComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new AgentVictoryLogic(),
                    new AgentHumanAILogic(),
                    new MissionAgentPanicHandler(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerAdminComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new CaptainScoreboardData()),
                    new EquipmentControllerLeaveLogic(),
                    new MultiplayerPreloadHelper(),
                    new TeamKillTrackerBehavior()
            };

            if (Config.Instance.ActivateSAE)
            {
                behaviors.Add(new SaeBehavior());
            }

            return behaviors;
        }
    }
}
