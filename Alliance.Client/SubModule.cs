using Alliance.Client.Core.Providers;
using Alliance.Client.Extensions.NativeIntermissionVote.Handlers;
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
using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Patch;
using Alliance.Common.Utilities;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using Module = TaleWorlds.MountAndBlade.Module;

namespace Alliance.Client
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Client";

		protected override void OnSubModuleLoad()
		{
			// Register and initialize Key Binder
			List<Assembly> assemblies = new List<Assembly>
			{
				Assembly.GetAssembly(typeof(Common.SubModule)),
				Assembly.GetAssembly(typeof(Client.SubModule))
			};
			KeyBinder.Initialize(assemblies);

			Client_ActionFactory.Initialize();

			// Apply Harmony patches
			DirtyCommonPatcher.Patch();
			DirtyClientPatcher.Patch();

			KeyBinder.RegisterContexts();

			AddGameModes();

			// Load our own list of orders for troop control (enable troop transfer)
			VisualOrderFactory.RegisterProvider(new AllianceVisualOrderProvider());
		}

		protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
		{
			// TODO : Check which limits still need to be increased after 1.2
			// Increase native network compression limits to prevent crashes
			DirtyCommonPatcher.IncreaseNativeLimits();

			// Initialize animation system and all the game animations
			AnimationSystem.Instance.Init();
		}

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
		}

		public override void OnGameInitializationFinished(Game game)
		{
			// Load ExtendedCharacter.xml into usable ExtendedCharacterObjects
			ExtendedXMLLoader.Init();

			SceneList.Initialize();
			ScenarioPlayer.Initialize();
			NativeIntermissionVoteHandler.Register();
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