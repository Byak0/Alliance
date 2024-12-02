using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
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
				// Native duel views
				new EquipmentSelectionView(),
				MultiplayerViewCreator.CreateMultiplayerCultureSelectUIHandler(),
				ViewCreator.CreateMissionAgentStatusUIHandler(mission),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, true),
				MultiplayerViewCreator.CreateLobbyEquipmentUIHandler(),
				MultiplayerViewCreator.CreateMissionMultiplayerDuelUI(),
			});
			return views.ToArray();
		}
	}
}
