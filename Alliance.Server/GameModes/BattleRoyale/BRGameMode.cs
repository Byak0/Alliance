using Alliance.Common.Extensions.ShrinkingZone.Behaviors;
using Alliance.Common.GameModes.BattleRoyale.Behaviors;
using Alliance.Server.GameModes.BattleRoyale.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Server.GameModes.BattleRoyale
{
	public class BRGameMode : MissionBasedMultiplayerGameMode
	{
		public BRGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("BattleRoyale", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new TDMScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new BRBehavior(),
				new BRCommonBehavior(),
				new ShrinkingZoneBehavior(),
				new SpawnComponent(new BRSpawnFrameBehavior(), new BRSpawningBehavior()),
			});
			return behaviors;
		}
	}
}