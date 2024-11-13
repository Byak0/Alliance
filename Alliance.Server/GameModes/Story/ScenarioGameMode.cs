using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Models;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Server.Extensions.FlagsTracker.Behaviors;
using Alliance.Server.GameModes.Story.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

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
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new PvCScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new ScenarioBehavior(),
				new ScenarioClientBehavior(),
				new ScenarioRespawnBehavior(),
				new SpawnComponent(new ScenarioDefaultSpawnFrameBehavior(), new ScenarioSpawningBehavior()),
				new FormationBehavior(),
				new ObjectivesBehavior(ScenarioManagerServer.Instance),
				new FlagTrackerBehavior(),
				new CapturableZoneBehavior(),

				// Native behaviors
				new MultiplayerTeamSelectComponent()
			});
			return behaviors;
		}
	}
}
