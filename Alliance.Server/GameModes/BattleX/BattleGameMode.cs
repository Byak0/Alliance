using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.Captain.Behaviors;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using Alliance.Server.GameModes.PvC.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Server.GameModes.BattleX
{
	public class BattleGameMode : MissionBasedMultiplayerGameMode
	{
		public BattleGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("BattleX", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new BattleScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new FormationBehavior(),
				new ALMissionMultiplayerFlagDomination(MultiplayerGameType.Battle),
				new ALMissionMultiplayerFlagDominationClient(),
				new SpawnComponent(new PvCSpawnFrameBehavior(), new PvCSpawningBehavior()),

				// Native battle behaviors
				new MultiplayerRoundController(),
				new MultiplayerWarmupComponent(),
				new MultiplayerTeamSelectComponent(),
				new AgentVictoryLogic()
			});
			return behaviors;
		}
	}
}
