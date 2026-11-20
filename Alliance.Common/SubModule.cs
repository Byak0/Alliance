using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using Alliance.Common.Extensions.BuildSystem.Configuration;
using Alliance.Common.GameModels;
using BehaviorTreeWrapper;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Common";
		public static string CurrentModuleName = "Alliance";

		protected override void OnSubModuleLoad()
		{
			BuildPrefabCatalogManager.Initialize();
			Log("Alliance.Common initialized", LogLevel.Debug);
		}

		protected override void OnBeforeInitialModuleScreenSetAsRoot()
		{
		}

		public override void OnGameInitializationFinished(Game game)
		{
		}

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			mission.AddMissionBehavior(new BehaviorTreeMissionLogic());
			mission.AddMissionBehavior(new AL_MissionAgentHandler());
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			// Add our custom GameModels 
			//gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
			gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
			base.OnGameStart(game, gameStarter);

			var ExtendedAgentStatCalculateModel = CoreUtils.GetGameModel<AgentStatCalculateModel>(gameStarter);
			gameStarter.AddModel(new ExtendedAgentStatCalculateModel(ExtendedAgentStatCalculateModel));
		}
	}
}