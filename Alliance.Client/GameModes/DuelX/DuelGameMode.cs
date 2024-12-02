using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Client.GameModes.DuelX
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
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new DuelScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Native duel behavior
				new MissionMultiplayerGameModeDuelClient()
			});
			return behaviors;
		}
	}
}
