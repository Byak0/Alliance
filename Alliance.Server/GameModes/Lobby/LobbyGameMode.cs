using Alliance.Common.GameModes.Lobby.Behaviors;
using Alliance.Server.Extensions.GameModeMenu.Behaviors;
using Alliance.Server.GameModes.Lobby.Behaviors;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;

namespace Alliance.Server.GameModes.Lobby
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
			List<MissionBehavior> behaviors = DefaultServerBehaviors.GetDefaultBehaviors(new TDMScoreboardData());
			behaviors.AppendList(new List<MissionBehavior>
			{
				// Custom behaviors
				new LobbyBehavior(),
				new LobbyClientBehavior(),
				new SpawnComponent(new LobbySpawnFrameBehavior(), new LobbySpawningBehavior()),
				new PollBehavior()
			});
			return behaviors;
		}
	}
}
