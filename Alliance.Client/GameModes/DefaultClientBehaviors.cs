using Alliance.Client.Core;
using Alliance.Client.Extensions.FakeArmy.Behaviors;
using Alliance.Client.Extensions.VOIP.Behaviors;
using Alliance.Client.Patch.Behaviors;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Client.GameModes
{
	public static class DefaultClientBehaviors
	{
		/// <summary>
		/// List of default behaviors for the client, included in every game mode.
		/// </summary>
		public static List<MissionBehavior> GetDefaultBehaviors(IScoreboardData scoreboardData)
		{
			return new List<MissionBehavior>()
			{
				// Default behaviors from native
				MissionLobbyComponent.CreateBehavior(),
				new MissionScoreboardComponent(scoreboardData),
				new MultiplayerTimerComponent(),
				new MultiplayerMissionAgentVisualSpawnComponent(),
				new ConsoleMatchStartEndHandler(),
				new MissionLobbyEquipmentNetworkComponent(),
				MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
				new MultiplayerAchievementComponent(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new MultiplayerPollComponent(),
				new MultiplayerAdminComponent(),
				new MultiplayerGameNotificationsComponent(),
				new MissionOptionsComponent(),
				new EquipmentControllerLeaveLogic(),
				new MissionRecentPlayersComponent(),
				new MultiplayerPreloadHelper(),

				// Default behaviors from Alliance
				new ClientAutoHandler(), // Core behavior, handle network message redirections
				new UsableEntityBehavior(),
				new PBVoiceChatHandlerClient(),
				new FakeArmyBehavior(),
				new AllianceAgentVisualSpawnComponent(),
				new AdvancedCombatBehavior(),
			};
		}
	}
}
