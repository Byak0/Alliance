using Alliance.Server.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.BattleX
{
	public class DuelGameMode : MissionBasedMultiplayerGameMode
	{
		public DuelGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("DuelX", new MissionInitializerRecord(scene), delegate (Mission missionController)
			{
				return new MissionBehavior[]
				{
					new AllianceLobbyComponent(),

					//MissionLobbyComponent.CreateBehavior(),
					new MissionMultiplayerDuel(),
					new MissionMultiplayerGameModeDuelClient(),
					new MultiplayerTimerComponent(),
					new SpawnComponent(new DuelSpawnFrameBehavior(), new DuelSpawningBehavior()),
					new MissionLobbyEquipmentNetworkComponent(),
					new MissionHardBorderPlacer(),
					new MissionBoundaryPlacer(),
					new MissionBoundaryCrossingHandler(),
					new MultiplayerPollComponent(),
					new MultiplayerAdminComponent(),
					new MultiplayerGameNotificationsComponent(),
					new MissionOptionsComponent(),
					new MissionScoreboardComponent(new DuelScoreboardData()),
					new MissionAgentPanicHandler(),
					new AgentHumanAILogic(),
					new EquipmentControllerLeaveLogic(),
					new MultiplayerPreloadHelper()
				};
			}, true, true);
		}
	}
}
