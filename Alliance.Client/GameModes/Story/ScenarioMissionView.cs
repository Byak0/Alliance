using Alliance.Client.Extensions.ExNativeUI.Views;
using Alliance.Client.Extensions.FlagsTracker.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using Alliance.Client.GameModes.Story.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.Story
{
    [ViewCreatorModule]
    public class ScenarioMissionView
    {
        [ViewMethod("Scenario")]
        public static MissionView[] OpenPvCMission(Mission mission)
        {
            List<MissionView> list = new List<MissionView>
            {
                // Custom views
                new SpawnTroopsView(),
                new KillNotificationView(),
                new LobbyEquipmentView(),
                new AgentStatusView(),
                new TeamSelectView(),
                new FormationStatusView(),
                new MarkerUIHandlerView(),
                new HUDExtensionUIHandlerView(),
                new HideWeaponTrail(),
                new GameModeMenuView(),
                new ScenarioView(),
                new CaptureMarkerUIView(),

                // Native views from Captain mode
                ViewCreator.CreateMissionServerStatusUIHandler(),
                ViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
                ViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionMultiplayerEscapeMenu("Scenario"),
                ViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                ViewCreator.CreatePollProgressUIHandler(),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission)
            };
            if (!GameNetwork.IsClient)
            {
                list.Add(ViewCreator.CreateMultiplayerAdminPanelUIHandler());
            }
            list.Add(ViewCreator.CreateMissionBoundaryCrossingView());
            list.Add(new MissionBoundaryWallView());
            list.Add(new SpectatorCameraView());

            return list.ToArray();
        }
    }
}
