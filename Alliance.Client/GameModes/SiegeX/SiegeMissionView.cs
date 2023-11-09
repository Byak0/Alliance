using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.SiegeX
{
    [ViewCreatorModule]
    public class SiegeMissionView
    {
        [ViewMethod("SiegeX")]
        public static MissionView[] OpenSiegeMission(Mission mission)
        {
            List<MissionView> list = new List<MissionView>
            {
                new SpawnTroopsView(),
                new GameModeMenuView(),
                new FormationStatusView(),

                ViewCreator.CreateMissionServerStatusUIHandler(),
                ViewCreator.CreateMissionMultiplayerPreloadView(mission),
                ViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                ViewCreator.CreateMissionMultiplayerEscapeMenu("Siege"),
                ViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                ViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                ViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                ViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                ViewCreator.CreateLobbyEquipmentUIHandler(),
                ViewCreator.CreatePollProgressUIHandler(),
                ViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
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
            return list.ToArray();
        }
    }
}
