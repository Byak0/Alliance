using Alliance.Common.Core.ExtendedXML;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Common.Patch.HarmonyPatch;
using Alliance.Common.Utilities;
using Alliance.Server.Core.Configuration;
using Alliance.Server.Core.Security;
using Alliance.Server.GameModes.BattleRoyale;
using Alliance.Server.GameModes.BattleX;
using Alliance.Server.GameModes.CaptainX;
using Alliance.Server.GameModes.CvC;
using Alliance.Server.GameModes.DuelX;
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
		public const string PlayerSpawnMenuFilePath = "spawn_preset_lobby_inf.xml";

		protected override void OnSubModuleLoad()
		{
			// Initialize player roles and access level
			SecurityInitializer.Init();

			Server_ActionFactory.Initialize();

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

			ScenarioManagerServer.Initialize();

			// Initialize the player spawn menu
			if (PlayerSpawnMenu.TryLoadFromFile(PlayerSpawnMenuFilePath, out PlayerSpawnMenu newMenu))
			{
				PlayerSpawnMenu.Instance = newMenu;
				Log($"Alliance - Loaded PlayerSpawnMenu succesfully with {PlayerSpawnMenu.Instance.Teams.Count} teams.", LogLevel.Information);
			}
			else
			{
				PlayerSpawnMenu.Instance = new PlayerSpawnMenu();
				Log($"Alliance - Failed to load PlayerSpawnMenu from {PlayerSpawnMenuFilePath}. Using default menu.", LogLevel.Warning);
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			// Late patching, patching earlier causes issues with Voice type
			Patch_AdvancedCombat.LatePatch();

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
			Module.CurrentModule.AddMultiplayerGameMode(new DuelGameMode("DuelX"));
		}
	}
}