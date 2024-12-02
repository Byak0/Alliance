using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using Alliance.Common.GameModes.CvC.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Client.GameModes.CaptainX
{
	public class CaptainGameMode : MissionBasedMultiplayerGameMode
	{
		public CaptainGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("CaptainX", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new CaptainScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new CvCGameModeClientBehavior(),
				new ALMissionMultiplayerFlagDominationClient(),

				// Native captain behaviors
				new MultiplayerWarmupComponent(),
				new MultiplayerRoundComponent(),
				new MultiplayerTeamSelectComponent(),
				new AgentVictoryLogic(),
			});
			return behaviors;
		}
	}
}
