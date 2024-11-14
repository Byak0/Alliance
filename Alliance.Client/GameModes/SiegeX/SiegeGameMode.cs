using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Siege.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Client.GameModes.SiegeX
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
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new SiegeScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new PvCMissionMultiplayerSiegeClient(),

				// Native siege behaviors
				new MultiplayerWarmupComponent(),
			});
			return behaviors;
		}
	}
}
