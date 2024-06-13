using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.ExNativeUI.SpectatorView.Views;
using Alliance.Client.Extensions.FlagsTracker.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
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
			List<MissionView> missionViews = new List<MissionView>
			{
				// Custom views
				new EquipmentSelectionView(),
				new AgentStatusView(),
				//new TeamSelectView(),
				new FormationStatusView(),
				//new MarkerUIHandlerView(),
				new HUDExtensionUIHandlerView(),
				new ScenarioView(),
				new CaptureMarkerUIView(),
				new SpectatorView(),

				// Native views from Captain mode
				MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
				MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
				MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipmentController(mission),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu("Scenario"),
				MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateOrderTroopPlacerView(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreatePollProgressUIHandler(),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
				MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				new MissionBoundaryWallView()
				//new SpectatorCameraView()
			};

			return missionViews.ToArray();
		}
	}
}
