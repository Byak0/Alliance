using Alliance.Client.Extensions.AgentsCount.Views;
using Alliance.Client.Extensions.ExNativeUI.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
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
                ViewCreator.CreateMissionServerStatusUIHandler(),
                ViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
                ViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionMultiplayerEscapeMenu("PvC"),
                ViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                ViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                ViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                //list.Add(ViewCreator.CreateMultiplayerEndOfBattleUIHandler());
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
            //list.Add(new SpectatorCameraView());
            list.Add(new SpectatorView());

            return list.ToArray();
        }
    }
}
