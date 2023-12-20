using Alliance.Client.Core;
using Alliance.Client.Core.KeyBinder;
using Alliance.Client.Extensions.AdminMenu.Views;
using Alliance.Client.Extensions.AnimationPlayer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.SAE.Behaviors;
using Alliance.Client.Extensions.TroopSpawner.Views;
using Alliance.Client.Extensions.UsableEntity.Views;
using Alliance.Client.Extensions.Vehicles.Views;
using Alliance.Client.GameModes.BattleRoyale;
using Alliance.Client.GameModes.BattleX;
using Alliance.Client.GameModes.CaptainX;
using Alliance.Client.GameModes.Lobby;
using Alliance.Client.GameModes.PvC;
using Alliance.Client.GameModes.SiegeX;
using Alliance.Client.GameModes.Story;
using Alliance.Client.Patch;
using Alliance.Common.Core.ExtendedCharacter;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Common.Utilities;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client
{
    public class SubModule : MBSubModuleBase
    {
        public const string ModuleId = "Alliance.Client";

        protected override void OnSubModuleLoad()
        {
            // Register and initialize Key Binder
            KeyBinder.Initialize();

            // Apply Harmony patches
            DirtyCommonPatcher.Patch();
            DirtyClientPatcher.Patch();

            AddGameModes();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            // TODO : Check if can be init there or need to be init later in the MissionView?
            // Initialize animation system and all the game animations
            AnimationSystem.Instance.Init();

            SceneList.Initialize();

            mission.AddMissionBehavior(new ClientAutoHandler());
            mission.AddMissionBehavior(new AdminSystem());
            mission.AddMissionBehavior(new AnimationView());
            mission.AddMissionBehavior(new SpawnTroopsView());
            mission.AddMissionBehavior(new VehicleView());
            mission.AddMissionBehavior(new UsableEntityBehavior());
            mission.AddMissionBehavior(new UsableEntityView());
            mission.AddMissionBehavior(new SaeBehavior());
            mission.AddMissionBehavior(new GameModeMenuView());

            Log("Alliance initialized.", LogLevel.Debug);
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            // TODO : Check which limits still need to be increased after 1.2
            // Increase native network compression limits to prevent crashes
            DirtyCommonPatcher.IncreaseNativeLimits();
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
            //gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
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