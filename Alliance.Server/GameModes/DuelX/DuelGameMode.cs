using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Server.GameModes.BattleX
{
	public class DuelGameMode : MissionBasedMultiplayerGameMode
	{
		public DuelGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("DuelX", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new DuelScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Native duel behaviors
				new MissionMultiplayerDuel(),
				new MissionMultiplayerGameModeDuelClient(),
				new SpawnComponent(new DuelSpawnFrameBehavior(), new DuelSpawningBehavior()),
				new MissionAgentPanicHandler()
			});
			return behaviors;
		}
	}
}
