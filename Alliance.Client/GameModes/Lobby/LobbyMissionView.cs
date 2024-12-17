using System.Collections.Generic;
using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using TaleWorlds.Library;
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
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Lobby");
			views.AppendList(new List<MissionView>
			{
				// Custom views
				new AgentStatusView(),
				
				// Native views
				MultiplayerViewCreator.CreateMissionMultiplayerFFAView(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler()
			});
			return views.ToArray();
		}
	}
}
