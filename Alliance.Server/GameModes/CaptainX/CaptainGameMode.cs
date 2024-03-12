using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.Extensions.VOIP.Behaviors;
using Alliance.Common.GameModes.Captain.Behaviors;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using Alliance.Server.Patch.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.CaptainX
{
    public class CaptainGameMode : MissionBasedMultiplayerGameMode
    {
        public CaptainGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("CaptainX", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return getMissionBehaviors();
            }, true, true);
        }

        private List<MissionBehavior> getMissionBehaviors()
        {
            List<MissionBehavior> behaviors = new List<MissionBehavior>()
            {
                    new AllianceLobbyComponent(),
                    new FormationBehavior(),
                    new VoipHandler(),

                    new ALMissionMultiplayerFlagDomination(MultiplayerGameType.Captain),
                    new PvCMissionMultiplayerGameModeFlagDominationClient(),
                    new MultiplayerRoundController(),
                    new MultiplayerWarmupComponent(),
                    new MultiplayerTimerComponent(),
                    new SpawnComponent(new PvCFlagDominationSpawnFrameBehavior(), new PvCFlagDominationSpawningBehavior()),
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
