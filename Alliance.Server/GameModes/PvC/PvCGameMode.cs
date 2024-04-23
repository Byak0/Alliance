using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Behaviors;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.Extensions.SimpleRespawn.Behaviors;
using Alliance.Server.GameModes.PvC.Behaviors;
using Alliance.Server.Patch.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
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
            MissionState.OpenNew("PvC", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
        }

        private List<MissionBehavior> GetMissionBehaviors()
        {
            List<MissionBehavior> behaviors = new List<MissionBehavior>
            {
                    new AllianceLobbyComponent(),
                    new FormationBehavior(),
                    new PvCGameModeBehavior(MultiplayerGameType.Captain),
                    new MultiplayerRoundController(),
                    new PvCGameModeClientBehavior(),
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
                    new MultiplayerPreloadHelper()
            };

            if (Config.Instance.ActivateSAE)
            {
                behaviors.Add(new SaeBehavior());
            }

            return behaviors;
        }
    }
}
