﻿using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
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
	}
}