using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Models;
using Alliance.Common.GameModes.Story.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Client.GameModes.Story
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
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new PvCScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new ScenarioClientBehavior(),
				new FormationBehavior(),
				new ObjectivesBehavior(ScenarioPlayer.Instance),

				// Native behaviors from Captain mode
				new MultiplayerAchievementComponent(),
				new AgentVictoryLogic(),
				new MultiplayerTeamSelectComponent(),
			});
			return behaviors;
		}
	}
}