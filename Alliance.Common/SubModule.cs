using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
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

		protected override void OnApplicationTick(float dt)
		{
			if (Mission.Current != null) SpatialGrid.UpdateGrid(Mission.Current.Agents);
		}

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			mission.AddMissionBehavior(new BehaviorTreeMissionLogic());
			mission.AddMissionBehavior(new AL_MissionAgentHandler());
		}
	}
}