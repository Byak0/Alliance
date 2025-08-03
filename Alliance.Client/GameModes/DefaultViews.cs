using Alliance.Client.Extensions.ExNativeUI.MissionMainAgentEquipmentController.MissionViews;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;


namespace Alliance.Client.GameModes
{
	public static class DefaultViews
	{
		/// <summary>
		/// List of default views for the client, included in every game mode.
		/// MissionViews with the [DefaultView] attribute are automatically included, so they don't need to be added here.
		/// </summary>
		public static List<MissionView> GetDefaultViews(Mission mission, string gameMode)
		{
			return new List<MissionView>()
			{
				// Custom views
				new MissionGauntletMainAgentEquipmentControllerViewCustom(),
				
				// Default views from native
				MultiplayerViewCreator.CreateMissionServerStatusUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerAdminPanelUIHandler(),
				MultiplayerViewCreator.CreateMissionMultiplayerPreloadView(mission),
				MultiplayerViewCreator.CreateMissionKillNotificationUIHandler(),
				ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
				MultiplayerViewCreator.CreateMissionMultiplayerEscapeMenu(gameMode),
				MultiplayerViewCreator.CreatePollProgressUIHandler(),
				ViewCreator.CreateOptionsUIHandler(),
				ViewCreator.CreateMissionMainAgentEquipDropView(mission),
				ViewCreator.CreateMissionBoundaryCrossingView(),
				MultiplayerViewCreator.CreateMissionFlagMarkerUIHandler(),
				new MissionBoundaryWallView(),
				new MissionItemContourControllerView(),
				new MissionAgentContourControllerView()
			};
		}
	}
}
