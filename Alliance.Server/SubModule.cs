using Alliance.Common.Core.ExtendedXML;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Common.Utilities;
using Alliance.Server.Core;
using Alliance.Server.Core.Configuration;
using Alliance.Server.Core.Configuration.Behaviors;
using Alliance.Server.Core.Security;
using Alliance.Server.Core.Security.Behaviors;
using Alliance.Server.Extensions.AdminMenu.Behaviors;
using Alliance.Server.Extensions.AIBehavior.Behaviors;
using Alliance.Server.Extensions.Animals.Behaviors;
using Alliance.Server.Extensions.ClassLimiter.Behaviors;
using Alliance.Server.Extensions.DieUnderWater.Behaviors;
using Alliance.Server.Extensions.FakeArmy.Behaviors;
using Alliance.Server.Extensions.TroopSpawner.Behaviors;
using Alliance.Server.Extensions.WargAttack.Behavior;
using Alliance.Server.GameModes.BattleRoyale;
using Alliance.Server.GameModes.BattleX;
using Alliance.Server.GameModes.CaptainX;
using Alliance.Server.GameModes.CvC;
using Alliance.Server.GameModes.Lobby;
using Alliance.Server.GameModes.PvC;
using Alliance.Server.GameModes.SiegeX;
using Alliance.Server.GameModes.Story;
using Alliance.Server.GameModes.Story.Actions;
using Alliance.Server.Patch;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Server";
		public const string RolesFilePath = "./alliance_roles.txt";
		public const string ConfigFilePath = "./alliance_config.txt";

		protected override void OnSubModuleLoad()
		{
			// Initialize player roles and access level
			SecurityInitializer.Init();

			// Init for Scenario
			Server_ActionFactory.Initialize();
			ScenarioManagerServer.Initialize();

			// Initialize mod configuration
			ConfigInitializer.Init();

			// Apply Harmony patches
			DirtyCommonPatcher.Patch();
			DirtyServerPatcher.Patch();

			AddGameModes();
		}

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			// Initialize animation system and all the game animations
			AnimationSystem.Instance.Init();

			SceneList.Initialize();
			ClassLimiterModel.Instance.Init();

			AddCommonBehaviors(mission);

			// Apply additional native fixes through MissionBehaviors
			DirtyServerPatcher.AddFixBehaviors(mission);

			Log("Alliance behaviors initialized.", LogLevel.Debug);
		}

		protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
		{
			// TODO : Check which limits still need to be increased after 1.2
			// Increase native network compression limits to prevent crashes
			DirtyCommonPatcher.IncreaseNativeLimits();

			// Add player connection watcher for auto-kick
			game.AddGameHandler<PlayerConnectionWatcher>();
		}

		public override void OnGameInitializationFinished(Game game)
		{
			// Load ExtendedCharacter.xml into usable ExtendedCharacterObjects
			ExtendedXMLLoader.Init();
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			// Add our custom GameModels 
			gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
			gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
		}

		public override void OnGameEnd(Game game)
		{
			game.RemoveGameHandler<PlayerConnectionWatcher>();
		}

		private void AddGameModes()
		{
			Module.CurrentModule.AddMultiplayerGameMode(new LobbyGameMode("Lobby"));
			Module.CurrentModule.AddMultiplayerGameMode(new BRGameMode("BattleRoyale"));
			Module.CurrentModule.AddMultiplayerGameMode(new PvCGameMode("PvC"));
			Module.CurrentModule.AddMultiplayerGameMode(new CvCGameMode("CvC"));
			Module.CurrentModule.AddMultiplayerGameMode(new ScenarioGameMode("Scenario"));
			Module.CurrentModule.AddMultiplayerGameMode(new CaptainGameMode("CaptainX"));
			Module.CurrentModule.AddMultiplayerGameMode(new BattleGameMode("BattleX"));
			Module.CurrentModule.AddMultiplayerGameMode(new SiegeGameMode("SiegeX"));
		}

		private void AddCommonBehaviors(Mission mission)
		{
			mission.AddMissionBehavior(new SyncRolesBehavior());
			mission.AddMissionBehavior(new SyncConfigBehavior());
			mission.AddMissionBehavior(new ServerAutoHandler());
			mission.AddMissionBehavior(new UsableEntityBehavior());
			mission.AddMissionBehavior(new TroopSpawnerBehavior());
			mission.AddMissionBehavior(new ClassLimiterBehavior());
			mission.AddMissionBehavior(new BattlePowerCalculationLogic());
			mission.AddMissionBehavior(new ALGlobalAIBehavior());
			mission.AddMissionBehavior(new DieUnderWaterBehavior());
			mission.AddMissionBehavior(new FakeArmyBehavior());
			mission.AddMissionBehavior(new RespawnBehavior());
			mission.AddMissionBehavior(new WargBehavior());
			mission.AddMissionBehavior(new AnimalBehavior());
		}
	}
}