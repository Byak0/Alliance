using Alliance.Common.Extensions.ShrinkingZone.Behaviors;
using Alliance.Common.GameModes.BattleRoyale.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Client.GameModes.BattleRoyale
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
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new TDMScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new BRCommonBehavior(),
				new ShrinkingZoneBehavior(),
			});
			return behaviors;
		}
	}
}
