using Alliance.Client.Core;
using Alliance.Client.Core.KeyBinder;
using Alliance.Client.GameModes.BattleRoyale;
using Alliance.Client.GameModes.BattleX;
using Alliance.Client.GameModes.CaptainX;
using Alliance.Client.GameModes.CvC;
using Alliance.Client.GameModes.Lobby;
using Alliance.Client.GameModes.PvC;
using Alliance.Client.GameModes.SiegeX;
using Alliance.Client.GameModes.Story;
using Alliance.Client.Patch;
using Alliance.Common.Core.ExtendedCharacter;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.ClassLimiter.Models;
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

            KeyBinder.RegisterContexts();

            AddGameModes();
        }

        protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
        {
            // TODO : Check which limits still need to be increased after 1.2
            // Increase native network compression limits to prevent crashes
            DirtyCommonPatcher.IncreaseNativeLimits();
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            // Initialize animation system and all the game animations
            AnimationSystem.Instance.Init();

            SceneList.Initialize();
            ClassLimiterModel.Instance.Init();

            AddCommonBehaviors(mission);

            Log("Alliance behaviors initialized.", LogLevel.Debug);
        }

        public override void OnGameInitializationFinished(Game game)
        {
            // Load ExtendedCharacter.xml into usable ExtendedCharacterObjects
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
            Module.CurrentModule.AddMultiplayerGameMode(new CvCGameMode("CvC"));
            Module.CurrentModule.AddMultiplayerGameMode(new ScenarioGameMode("Scenario"));
            Module.CurrentModule.AddMultiplayerGameMode(new CaptainGameMode("CaptainX"));
            Module.CurrentModule.AddMultiplayerGameMode(new BattleGameMode("BattleX"));
            Module.CurrentModule.AddMultiplayerGameMode(new SiegeGameMode("SiegeX"));
        }

        /// <summary>
        /// Add common behaviors from Alliance used by all GameModes.
        /// </summary>
        public void AddCommonBehaviors(Mission mission)
        {
            mission.AddMissionBehavior(new ClientAutoHandler());
            mission.AddMissionBehavior(new UsableEntityBehavior());
        }
    }
}