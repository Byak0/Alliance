using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.ExNativeUI.KillNotification.Views;
using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.ExNativeUI.MarkerUIHandler.Views;
using Alliance.Client.Extensions.ExNativeUI.TeamSelect.Views;
using Alliance.Client.Extensions.FlagsTracker.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using Alliance.Client.Extensions.WeaponTrailHider.Views;
using Alliance.Client.GameModes.Story.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
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
                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Scenario"),
                MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView(),
                new SpectatorCameraView()
            };

            return list.ToArray();
        }
    }
}
