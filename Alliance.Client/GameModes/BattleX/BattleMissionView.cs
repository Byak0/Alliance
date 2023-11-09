using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.BattleX
{
    [ViewCreatorModule]
    public class BattleMissionView
    {
        [ViewMethod("BattleX")]
        public static MissionView[] OpenBattleMission(Mission mission)
        {
            List<MissionView> list = new List<MissionView>();
            list.Add(new SpawnTroopsView());
            list.Add(new GameModeMenuView());
            list.Add(new FormationStatusView());

            list.Add(ViewCreator.CreateLobbyEquipmentUIHandler());
            list.Add(ViewCreator.CreateMultiplayerFactionBanVoteUIHandler());
            list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
            list.Add(ViewCreator.CreateMissionMultiplayerPreloadView(mission));
            list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
            list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
            list.Add(ViewCreator.CreateMissionMultiplayerEscapeMenu("Battle"));
            list.Add(ViewCreator.CreateMultiplayerMissionOrderUIHandler(mission));
            list.Add(ViewCreator.CreateMissionAgentLabelUIHandler(mission));
            list.Add(ViewCreator.CreateOrderTroopPlacerView(mission));
            list.Add(ViewCreator.CreateMultiplayerTeamSelectUIHandler());
            list.Add(ViewCreator.CreateMissionScoreBoardUIHandler(mission, false));
            list.Add(ViewCreator.CreateMultiplayerEndOfRoundUIHandler());
            list.Add(ViewCreator.CreateMultiplayerEndOfBattleUIHandler());
            list.Add(ViewCreator.CreatePollProgressUIHandler());
            list.Add(new MissionItemContourControllerView());
            list.Add(new MissionAgentContourControllerView());
            list.Add(ViewCreator.CreateMissionKillNotificationUIHandler());
            list.Add(ViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler());
            list.Add(ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null));
            list.Add(ViewCreator.CreateMissionFlagMarkerUIHandler());
            list.Add(ViewCreator.CreateOptionsUIHandler());
            list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
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
