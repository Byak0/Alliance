using Alliance.Client.Extensions.AgentsCount.Views;
using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.ExNativeUI.KillNotification.Views;
using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.ExNativeUI.MarkerUIHandler.Views;
using Alliance.Client.Extensions.ExNativeUI.SpectatorView.Views;
using Alliance.Client.Extensions.ExNativeUI.TeamSelect.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using Alliance.Client.Extensions.WeaponTrailHider.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.PvC
{
    [ViewCreatorModule]
    public class PvCMissionView
    {
        [ViewMethod("PvC")]
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
                new AgentsCountView(),
                new MarkerUIHandlerView(),
                new HUDExtensionUIHandlerView(),
                new HideWeaponTrail(),
                new GameModeMenuView(),

                // Native views from Captain mode
				MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("PvC"),
                MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView(),
                new SpectatorView()
            };

            return list.ToArray();
        }
    }
}
