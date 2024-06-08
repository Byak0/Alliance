using Alliance.Client.Patch.Behaviors;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.PvC.Models;
using Alliance.Common.GameModes.Story.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Client.GameModes.Story
{
	public class ScenarioGameMode : MissionBasedMultiplayerGameMode
	{
		public ScenarioGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("Scenario", new MissionInitializerRecord(scene), delegate (Mission missionController)
			{
				return new MissionBehavior[]
				{
					MissionLobbyComponent.CreateBehavior(),
					
					// Custom components
					new ScenarioClientBehavior(),
					new MissionScoreboardComponent(new PvCScoreboardData()), // todo : replace with scenario infos
					new FormationBehavior(),
					new ObjectivesBehavior(ScenarioPlayer.Instance),
					new AllianceAgentVisualSpawnComponent(),
					new MultiplayerTeamSelectComponent(),

					// Native components from Captain mode
					new MultiplayerAchievementComponent(),
					new AgentVictoryLogic(),
					new MultiplayerTimerComponent(),
					new MultiplayerMissionAgentVisualSpawnComponent(),
					new ConsoleMatchStartEndHandler(),
					new MissionLobbyEquipmentNetworkComponent(),
					new MissionHardBorderPlacer(),
					new MissionBoundaryPlacer(),
					new MissionBoundaryCrossingHandler(),
					new MultiplayerPollComponent(),
					new MultiplayerAdminComponent(),
					new MultiplayerGameNotificationsComponent(),
					new MissionOptionsComponent(),
					new EquipmentControllerLeaveLogic(),
					new MissionRecentPlayersComponent(),
					new MultiplayerPreloadHelper()
				};
			}, true, true);
		}
	}
}