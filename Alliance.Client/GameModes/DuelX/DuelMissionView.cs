using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.DuelX
{
	[ViewCreatorModule]
	public class DuelMissionView
	{
		[ViewMethod("DuelX")]
		public static MissionView[] OpenDuelMission(Mission mission)
		{
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Duel");
			views.AppendList(new List<MissionView>
			{
				// Custom views
				new AgentStatusView(),
				
				// Native duel views
				MultiplayerViewCreator.CreateMultiplayerCultureSelectUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, true),
				MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
				MultiplayerViewCreator.CreateMissionMultiplayerDuelUI(),
			});
			return views.ToArray();
		}
	}
}
