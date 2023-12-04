using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.TroopSpawner.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
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
            List<MissionView> list = new List<MissionView>
            {
                new GameModeMenuView(),
                new SpawnTroopsView(),

                MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerFFAView(),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Lobby"),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, true),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView()
            };
            return list.ToArray();
        }
    }
}
