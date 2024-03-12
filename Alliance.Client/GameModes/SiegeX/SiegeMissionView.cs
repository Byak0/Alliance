using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
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
            List<MissionView> missionViews = new List<MissionView>
            {
                new EquipmentSelectionView(),
                new FormationStatusView(),

                MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
                MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
                MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
                ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Siege"),
                MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
                ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
                MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
                MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
                MultiplayerViewCreator.CreatePollProgressUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
                MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
                new MissionItemContourControllerView(),
                new MissionAgentContourControllerView(),
                MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
                ViewCreator.CreateOptionsUIHandler(),
                ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                ViewCreator.CreateMissionBoundaryCrossingView(),
                new MissionBoundaryWallView()
            };

            return missionViews.ToArray();
        }
    }
}
