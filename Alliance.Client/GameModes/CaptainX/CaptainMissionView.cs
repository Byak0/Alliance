using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.CaptainX
{
    [ViewCreatorModule]
    public class CaptainMissionView
    {
        [ViewMethod("CaptainX")]
        public static MissionView[] OpenCaptainMission(Mission mission)
        {
            List<MissionView> list = new List<MissionView>
            {
                new SpawnTroopsView(),
                new GameModeMenuView(),
                new FormationStatusView(),

                ViewCreator.CreateLobbyEquipmentUIHandler(),
                ViewCreator.CreateMissionServerStatusUIHandler(),
                ViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
                ViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionMultiplayerEscapeMenu("Captain"),
                ViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateOrderTroopPlacerView(mission),
                ViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                ViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                ViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                ViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                ViewCreator.CreatePollProgressUIHandler(),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                ViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateMissionFlagMarkerUIHandler(),
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
