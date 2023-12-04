using Alliance.Common.Core.ExtendedCharacter;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Server.Core;
using Alliance.Server.Core.Configuration;
using Alliance.Server.Core.Configuration.Behaviors;
using Alliance.Server.Core.Security;
using Alliance.Server.Core.Security.Behaviors;
using Alliance.Server.GameModes.BattleRoyale;
using Alliance.Server.GameModes.BattleX;
using Alliance.Server.GameModes.CaptainX;
using Alliance.Server.GameModes.Lobby;
using Alliance.Server.GameModes.PvC;
using Alliance.Server.GameModes.SiegeX;
using Alliance.Server.GameModes.Story;
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

            // Initialize mod configuration
            ConfigInitializer.Init();

            // Apply Harmony patches
            DirtyCommonPatcher.Patch();
            DirtyServerPatcher.Patch();

            AddGameModes();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            // Apply additional native fixes through MissionBehaviors
            DirtyServerPatcher.AddFixBehaviors(mission);

            // Synchronize player roles and access level
            mission.AddMissionBehavior(new SyncRolesBehavior());

            // Synchronize mod configuration
            mission.AddMissionBehavior(new SyncConfigBehavior());

            // Add main server handler
            mission.AddMissionBehavior(new ServerAutoHandler());

            // Initialize animation system and all the game animations
            AnimationSystem.Instance.Init();

            Log("Alliance initialized.", LogLevel.Debug);
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            // Increase native network compression limits to prevent crashes
            //DirtyCommonPatcher.IncreaseNativeLimits();

            // Add player connection watcher for auto-kick
            game.AddGameHandler<PlayerConnectionWatcher>();
        }

        public override void OnGameInitializationFinished(Game game)
        {
            // Load AllianceCharacter.xml into usable ExtendedCharacterObjects
            ExtendedCharacterLoader.Init();
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
            Module.CurrentModule.AddMultiplayerGameMode(new ScenarioGameMode("Scenario"));
            Module.CurrentModule.AddMultiplayerGameMode(new CaptainGameMode("CaptainX"));
            Module.CurrentModule.AddMultiplayerGameMode(new BattleGameMode("BattleX"));
            Module.CurrentModule.AddMultiplayerGameMode(new SiegeGameMode("SiegeX"));
        }
    }
}