using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Server.GameModes.CaptainX
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
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new CaptainScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new ALMissionMultiplayerFlagDomination(MultiplayerGameType.Captain),
				new ALMissionMultiplayerFlagDominationClient(),
				new SpawnComponent(new PvCFlagDominationSpawnFrameBehavior(), new PvCFlagDominationSpawningBehavior()),

				// Native captain behaviors
				new MultiplayerRoundController(),
				new MultiplayerWarmupComponent(),
				new MultiplayerTeamSelectComponent(),
				new AgentVictoryLogic(),
				new MissionAgentPanicHandler()
			});
			return behaviors;
		}
	}
}
