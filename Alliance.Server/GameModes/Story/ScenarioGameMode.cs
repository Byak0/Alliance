using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Models;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Server.Extensions.FlagsTracker.Behaviors;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.GameModes.Story.Behaviors;
using Alliance.Server.Patch.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.Story
{
	public class ScenarioGameMode : MissionBasedMultiplayerGameMode
	{
		public ScenarioGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("Scenario", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			List<MissionBehavior> behaviors = new List<MissionBehavior>()
			{
					new AllianceLobbyComponent(),

					// Custom components
					new SpawnComponent(new ScenarioDefaultSpawnFrameBehavior(), new ScenarioSpawningBehavior()),
					new ScenarioBehavior(),
					new ScenarioClientBehavior(),
					new MissionScoreboardComponent(new PvCScoreboardData()), // todo : replace with custom objectives ui
					new ScenarioRespawnBehavior(),
					//new PollBehavior(),
					new FormationBehavior(),
					new ObjectivesBehavior(ScenarioManagerServer.Instance),
					new FlagTrackerBehavior(),
					new CapturableZoneBehavior(),

					// Native components
					new MultiplayerTeamSelectComponent(),
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
					new MultiplayerPreloadHelper()
			};

			if (Config.Instance.ActivateSAE)
			{
				behaviors.Add(new SaeBehavior());
			}

			return behaviors;
		}
	}
}
