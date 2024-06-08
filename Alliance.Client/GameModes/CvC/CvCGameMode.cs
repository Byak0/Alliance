using Alliance.Client.Patch.Behaviors;
using Alliance.Common.Extensions.FormationEnforcer.Behavior;
using Alliance.Common.GameModes.CvC.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Client.GameModes.CvC
{
	public class CvCGameMode : MissionBasedMultiplayerGameMode
	{
		public CvCGameMode(string name) : base(name) { }

		[MissionMethod]
		public override void StartMultiplayerGame(string scene)
		{
			MissionState.OpenNew("CvC", new MissionInitializerRecord(scene), (missionController) => GetMissionBehaviors(), true, true);
		}

		private MissionBehavior[] GetMissionBehaviors()
		{
			MissionBehavior[] behaviors = new MissionBehavior[]
				{
					MissionLobbyComponent.CreateBehavior(),
					new FormationBehavior(),
					new AllianceAgentVisualSpawnComponent(),

					new MultiplayerAchievementComponent(),
					new MultiplayerWarmupComponent(),
					new CvCGameModeClientBehavior(),
					new MultiplayerRoundComponent(),
					new MultiplayerTimerComponent(),
					new MultiplayerMissionAgentVisualSpawnComponent(),
					new ConsoleMatchStartEndHandler(),
					new MissionLobbyEquipmentNetworkComponent(),
					new MultiplayerTeamSelectComponent(),
					new MissionHardBorderPlacer(),
					new MissionBoundaryPlacer(),
					new MissionBoundaryCrossingHandler(),
					new MultiplayerPollComponent(),
					new MultiplayerAdminComponent(),
					new MultiplayerGameNotificationsComponent(),
					new MissionOptionsComponent(),
					new MissionScoreboardComponent(new CaptainScoreboardData()),
					MissionMatchHistoryComponent.CreateIfConditionsAreMet(),
					new EquipmentControllerLeaveLogic(),
					new MissionRecentPlayersComponent(),
					new MultiplayerPreloadHelper()
				};

			return behaviors;
		}
	}
}
