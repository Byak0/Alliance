using Alliance.Client.Core;
using Alliance.Client.Core.KeyBinder;
using Alliance.Client.Extensions.AdminMenu.Views;
using Alliance.Client.Extensions.AnimationPlayer.Views;
using Alliance.Client.Extensions.GameModeMenu.Views;
using Alliance.Client.Extensions.SAE.Behaviors;
using Alliance.Client.Extensions.TroopSpawner.Views;
using Alliance.Client.Extensions.UsableEntity.Views;
using Alliance.Client.Extensions.Vehicles.Views;
using Alliance.Client.Extensions.VOIP.Views;
using Alliance.Client.Extensions.WeaponTrailHider.Views;
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
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
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

            mission.AddMissionBehavior(new ClientAutoHandler());
            mission.AddMissionBehavior(new UsableEntityBehavior());

            Log("Alliance initialized.", LogLevel.Debug);
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
            Module.CurrentModule.AddMultiplayerGameMode(new ScenarioGameMode("Scenario"));
            Module.CurrentModule.AddMultiplayerGameMode(new CaptainGameMode("CaptainX"));
            Module.CurrentModule.AddMultiplayerGameMode(new BattleGameMode("BattleX"));
            Module.CurrentModule.AddMultiplayerGameMode(new SiegeGameMode("SiegeX"));
        }

        /// <summary>
        /// Centralized list of common views from Alliance used by GameModes.
        /// </summary>
        public static List<MissionView> GetCommonViews()
        {
            return new List<MissionView>()
            {
                new VoipView(),
                new AdminSystem(),
                new AnimationView(),
                new SpawnTroopsView(),
                new VehicleView(),
                new UsableEntityView(),
                new SaeBehavior(),
                new GameModeMenuView(),
                new HideWeaponTrail()
            };
        }
    }
}