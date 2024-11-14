using Alliance.Client.GameModes.Story;
using Alliance.Common.GameModes.Lobby.Behaviors;
using Alliance.Common.GameModes.Story.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Client.GameModes.Lobby
{
	public class LobbyGameMode : MissionBasedMultiplayerGameMode
	{
		public LobbyGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("Lobby", new MissionInitializerRecord(scene), (Mission missionController) => GetMissionBehaviors(), true, true);
		}

		private List<MissionBehavior> GetMissionBehaviors()
		{
			// Default behaviors
			List<MissionBehavior> behaviors = DefaultClientBehaviors.GetDefaultBehaviors(new TDMScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
                new LobbyClientBehavior(),
				new ObjectivesBehavior(ScenarioPlayer.Instance),

				// Native behaviors
				new MultiplayerAchievementComponent(),
				new AgentVictoryLogic(),
				new MultiplayerTeamSelectComponent(),
			});
			return behaviors;
		}
	}
}
