using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
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
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Siege");
			views.AppendList(new List<MissionView>
			{
                // Custom views
                new EquipmentSelectionView(),
				new FormationStatusView(),
				new AgentStatusView(),

				// Native views
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler()
			});
			return views.ToArray();
		}
	}
}
