using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Server.Core;
using Alliance.Server.Core.Configuration.Behaviors;
using Alliance.Server.Core.Security.Behaviors;
using Alliance.Server.Extensions.AdminMenu.Behaviors;
using Alliance.Server.Extensions.AIBehavior.Behaviors;
using Alliance.Server.Extensions.Animals.Behaviors;
using Alliance.Server.Extensions.ClassLimiter.Behaviors;
using Alliance.Server.Extensions.DieUnderWater.Behaviors;
using Alliance.Server.Extensions.FakeArmy.Behaviors;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.Extensions.TroopSpawner.Behaviors;
using Alliance.Server.Extensions.WargAttack.Behavior;
using Alliance.Server.Patch.Behaviors;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes
{
	public static class DefaultServerBehaviors
	{
		/// <summary>
		/// List of default behaviors for the server, included in every game mode.
		/// </summary>
		public static List<MissionBehavior> GetDefaultBehaviors(IScoreboardData scoreboardData)
		{
			List<MissionBehavior> defaultBehaviors = new List<MissionBehavior>()
			{
				// Default behaviors from native
				new MissionScoreboardComponent(scoreboardData),
				new MultiplayerTimerComponent(),
				new AgentHumanAILogic(),
				new MissionLobbyEquipmentNetworkComponent(),
				new MissionHardBorderPlacer(),
				new MissionBoundaryPlacer(),
				new MissionBoundaryCrossingHandler(),
				new MultiplayerPollComponent(),
				new MultiplayerAdminComponent(),
				new MultiplayerGameNotificationsComponent(),
				new MissionOptionsComponent(),
				new EquipmentControllerLeaveLogic(),
				new MultiplayerPreloadHelper(),

				// Default behaviors from Alliance
				new ServerAutoHandler(), // Core behavior, handle network message redirections
				new AllianceLobbyComponent(),
				new SyncRolesBehavior(),
				new SyncConfigBehavior(),
				new UsableEntityBehavior(),
				new TroopSpawnerBehavior(),
				new ClassLimiterBehavior(),
				new BattlePowerCalculationLogic(),
				new ALGlobalAIBehavior(),
				new DieUnderWaterBehavior(),
				new FakeArmyBehavior(),
				new RespawnBehavior(),
				new WargBehavior(),
				new AnimalBehavior(),
				new ConditionsBehavior(),

				// Special MissionBehaviors fixing native bugs
				new NotAllPlayersJoinFixBehavior()
			};

			if (Config.Instance.ActivateSAE) defaultBehaviors.Add(new SaeBehavior());

			return defaultBehaviors;
		}
	}
}
