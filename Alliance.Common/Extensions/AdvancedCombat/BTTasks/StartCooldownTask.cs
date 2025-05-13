using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class StartCooldownTask : BTTask, IBTTimerBlackboard
	{
		public BTBlackboardValue<float> Timer { get; set; }

		public StartCooldownTask() : base()
		{
		}

		public override BTTaskStatus Execute()
		{
			Timer.SetValue(Mission.Current.GetMissionTimeInSeconds());
			return BTTaskStatus.FinishedWithTrue;
		}
	}
}
