using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.ExNativeUI.SpectatorView.Views;
using Alliance.Client.Extensions.FlagsTracker.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Client.GameModes.Story.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
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
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Scenario");
			views.AppendList(new List<MissionView>
			{
               // Custom views
				new EquipmentSelectionView(),
				new AgentStatusView(),
				new FormationStatusView(),
				new HUDExtensionUIHandlerView(),
				new ScenarioView(),
				new CaptureMarkerUIView(),
				new SpectatorView(),

				// Native views from Captain mode
				MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateOrderTroopPlacerView(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(),
				MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler()
			});
			return views.ToArray();
		}
	}
}
