using Alliance.Client.Core.KeyBinder;
using Alliance.Client.GameModes.BattleRoyale;
using Alliance.Client.GameModes.BattleX;
using Alliance.Client.GameModes.CaptainX;
using Alliance.Client.GameModes.CvC;
using Alliance.Client.GameModes.DuelX;
using Alliance.Client.GameModes.Lobby;
using Alliance.Client.GameModes.PvC;
using Alliance.Client.GameModes.SiegeX;
using Alliance.Client.GameModes.Story;
using Alliance.Client.GameModes.Story.Actions;
using Alliance.Client.Patch;
using Alliance.Common.Core.ExtendedXML;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Common.Utilities;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Client";

		protected override void OnSubModuleLoad()
		{
			// Register and initialize Key Binder
			KeyBinder.Initialize();

			Client_ActionFactory.Initialize();

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
		}

		public override void OnGameInitializationFinished(Game game)
		{
			// Load ExtendedCharacter.xml into usable ExtendedCharacterObjects
			ExtendedXMLLoader.Init();

			ClassLimiterModel.Instance.Init();
			SceneList.Initialize();
			ScenarioPlayer.Initialize();
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			// Add our custom GameModels 
			gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
			gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
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
			Module.CurrentModule.AddMultiplayerGameMode(new DuelGameMode("DuelX"));
		}
	}
}