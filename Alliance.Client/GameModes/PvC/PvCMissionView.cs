using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views;
using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.ExNativeUI.SpectatorView.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using Alliance.Common.Extensions.PlayerSpawn.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.PvC
{
	[ViewCreatorModule]
	public class PvCMissionView
	{
		[ViewMethod("PvC")]
		public static MissionView[] OpenPvCMission(Mission mission)
		{
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "PvC");
			views.AppendList(new List<MissionView>
			{
                // Custom views
                new EquipmentSelectionView(),
				new AgentStatusView(),
				new FormationStatusView(),
				new HUDExtensionUIHandlerView(),
				new SpectatorView(),
				new PlayerSpawnMenuView(),

				// Native views
				MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateOrderTroopPlacerView(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler()
			});
			return views.ToArray();
		}
	}
}
