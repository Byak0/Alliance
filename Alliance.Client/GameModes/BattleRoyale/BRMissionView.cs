using Alliance.Client.Extensions.AgentsCount.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.BattleRoyale
{
	[ViewCreatorModule]
	public class BRMissionView
	{
		[ViewMethod("BattleRoyale")]
		public static MissionView[] OpenBRMission(Mission mission)
		{
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "BattleRoyale");
			views.AppendList(new List<MissionView>
			{
				// Custom views
				new AgentsCountView(),

				// Native FFA views
				MultiplayerViewCreator.CreateMissionMultiplayerFFAView(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, true),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler()
			});
			return views.ToArray();
		}
	}
}
