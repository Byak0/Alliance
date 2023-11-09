using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.Lobby
{
    [ViewCreatorModule]
    public class LobbyMissionView
    {
        [ViewMethod("Lobby")]
        public static MissionView[] OpenLobbyMission(Mission mission)
        {
            List<MissionView> list = new List<MissionView>();
            list.Add(new GameModeMenuView());
            list.Add(new SpawnTroopsView());

            list.Add(ViewCreator.CreateMissionServerStatusUIHandler());
            list.Add(ViewCreator.CreateMissionMultiplayerPreloadView(mission));
            list.Add(ViewCreator.CreateMissionMultiplayerFFAView());
            list.Add(ViewCreator.CreateMissionKillNotificationUIHandler());
            list.Add(ViewCreator.CreateMissionAgentStatusUIHandler(mission));
            list.Add(ViewCreator.CreateMissionMainAgentEquipmentController(mission));
            list.Add(ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission));
            list.Add(ViewCreator.CreateMissionMultiplayerEscapeMenu("Lobby"));
            //list.Add(ViewCreator.CreateMultiplayerEndOfBattleUIHandler());
            list.Add(ViewCreator.CreateMissionScoreBoardUIHandler(mission, true));
            list.Add(ViewCreator.CreatePollProgressUIHandler());
            list.Add(ViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null));
            list.Add(ViewCreator.CreateOptionsUIHandler());
            list.Add(ViewCreator.CreateMissionMainAgentEquipDropView(mission));
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
