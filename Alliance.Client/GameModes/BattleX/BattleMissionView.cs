﻿using Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views;
using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views;
using Alliance.Client.Extensions.FormationEnforcer.Views;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.GameModes.BattleX
{
	[ViewCreatorModule]
	public class BattleMissionView
	{
		[ViewMethod("BattleX")]
		public static MissionView[] OpenBattleMission(Mission mission)
		{
			// Default views
			List<MissionView> views = DefaultViews.GetDefaultViews(mission, "Battle");
			views.AppendList(new List<MissionView>
			{
				// Custom views
				new EquipmentSelectionView(),
				new FormationStatusView(),
				new AgentStatusView(),

				// Native battle views
				MultiplayerViewCreator.CreateMultiplayerFactionBanVoteUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionOrderUIHandler(mission),
				ViewCreator.CreateMissionAgentLabelUIHandler(mission),
				ViewCreator.CreateOrderTroopPlacerView(mission),
				MultiplayerViewCreator.CreateMultiplayerTeamSelectUIHandler(),
				MultiplayerViewCreator.CreateMissionScoreBoardUIHandler(mission, false),
				MultiplayerViewCreator.CreateMultiplayerEndOfRoundUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerEndOfBattleUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionHUDExtensionUIHandler(),
				MultiplayerViewCreator.CreateMultiplayerMissionDeathCardUIHandler(null),
				new SpectatorCameraView()
			});
			return views.ToArray();
		}
	}
}
