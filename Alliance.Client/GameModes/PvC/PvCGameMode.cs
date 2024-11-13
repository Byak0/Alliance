using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Client.GameModes.PvC
{
	public class PvCGameMode : MissionBasedMultiplayerGameMode
	{
		public PvCGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("PvC", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new CaptainScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new PvCGameModeClientBehavior(),

				// Native behaviors
				new MultiplayerWarmupComponent(),
				new MultiplayerRoundComponent(),
				new MultiplayerTeamSelectComponent()
			});
			return behaviors;
		}
	}
}
