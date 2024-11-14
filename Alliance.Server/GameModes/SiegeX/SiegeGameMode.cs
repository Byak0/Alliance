using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Siege.Behaviors;
using Alliance.Server.GameModes.SiegeX.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Server.GameModes.SiegeX
{
	public class SiegeGameMode : MissionBasedMultiplayerGameMode
	{
		public SiegeGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("SiegeX", new MissionInitializerRecord(scene) { SceneUpgradeLevel = 3, SceneLevels = "" }, (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new SiegeScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new PvCMissionMultiplayerSiegeClient(),
				new SpawnComponent(new SiegeXSpawnFrameBehavior(), new SiegeXSpawningBehavior()),

				// Native siege behaviors
				new MissionMultiplayerSiege(),
				new MultiplayerWarmupComponent(),
				new MultiplayerTeamSelectComponent(),
				new MissionAgentPanicHandler()
			});
			return behaviors;
		}
	}
}
