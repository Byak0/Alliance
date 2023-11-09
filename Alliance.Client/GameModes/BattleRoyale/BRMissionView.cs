using Alliance.Client.Extensions.AgentsCount.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.BattleRoyale
{
    [ViewCreatorModule]
    public class BRMissionView
    {
        [ViewMethod("BattleRoyale")]
        public static MissionView[] OpenBRMission(Mission mission)
        {
            List<MissionView> list = new List<MissionView>
            {
                new GameModeMenuView(),
                new SpawnTroopsView(),
                new AgentsCountView(),

                ViewCreator.CreateMissionServerStatusUIHandler(),
                ViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionMultiplayerFFAView(),
                ViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionMultiplayerEscapeMenu("BattleRoyale"),
                ViewCreator.CreateMissionScoreBoardUIHandler(mission, true),
                ViewCreator.CreatePollProgressUIHandler(),
                ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView()
            };
            return list.ToArray();
        }
    }
}
