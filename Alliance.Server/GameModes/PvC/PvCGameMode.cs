using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Behaviors;
using Alliance.Server.GameModes.PvC.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Server.GameModes.PvC
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
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new CaptainScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new PvCGameModeBehavior(MultiplayerGameType.Captain),
				new PvCGameModeClientBehavior(),
				new SpawnComponent(new PvCSpawnFrameBehavior(), new PvCSpawningBehavior()),

				// Native captain behaviors
				new MultiplayerRoundController(),
				new MultiplayerTeamSelectComponent(),
				new MissionAgentPanicHandler(),
				new AgentVictoryLogic()
			});
			return behaviors;
		}
	}
}
