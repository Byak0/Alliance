using Alliance.Common.Core.ExtendedXML;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.GameModels;
using Alliance.Common.Patch;
using Alliance.Common.Patch.HarmonyPatch;
using Alliance.Common.Utilities;
using Alliance.SP.Patch;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.SP
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.SP";

		protected override void OnSubModuleLoad()
		{
			Common.SubModule.CurrentModuleName = ModuleId;

			// Apply Harmony patches
			DirtyCommonPatcher.Patch();

			DirtySPPatcher.Patch();
		}

		public override void OnGameInitializationFinished(Game game)
		{
			// Load ExtendedCharacter.xml into usable ExtendedCharacterObjects
			ExtendedXMLLoader.Init();

			ClassLimiterModel.Instance.Init();
			SceneList.Initialize();
		}

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			// Initialize animation system and all the game animations
			AnimationSystem.Instance.Init();

			mission.AddMissionBehavior(new CoreBehavior());
			mission.AddMissionBehavior(new AdvancedCombatBehavior());
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			// Late patching, patching earlier causes issues with Voice type
			Patch_AdvancedCombat.LatePatch();

			// Add our custom GameModels 
			gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
			gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
		}
	}
}