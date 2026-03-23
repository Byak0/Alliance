using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using Alliance.Common.GameModels;
using Alliance.Common.Patch.HarmonyPatch;
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
			// Late patching, patching earlier causes issues with Voice type
			Patch_AdvancedCombat.LatePatch();

			// Add our custom GameModels 
			//gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
			gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
			base.OnGameStart(game, gameStarter);

			var ExtendedAgentStatCalculateModel = CoreUtils.GetGameModel<CustomBattleAgentStatCalculateModel>(gameStarter);
			gameStarter.AddModel(new ExtendedAgentStatCalculateModel(ExtendedAgentStatCalculateModel));
		}
	}
}