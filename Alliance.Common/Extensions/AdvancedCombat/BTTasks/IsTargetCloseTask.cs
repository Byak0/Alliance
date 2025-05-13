using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class IsTargetCloseTask : BTTask, IBTCombatBlackboard
	{
		private readonly float maxRange;

		public BTBlackboardValue<Agent> Target { get; set; }
		public BTBlackboardValue<Agent> Agent { get; set; }

		public IsTargetCloseTask(float maxRange) : base()
		{
			this.maxRange = maxRange;
		}

		public override BTTaskStatus Execute()
		{
			Agent self = Agent.GetValue();
			Agent targetAgent = Target.GetValue();

			if (self == null || targetAgent == null || targetAgent.Health <= 0 || targetAgent.IsFadingOut()
				|| self.Position.Distance(targetAgent.Position) > maxRange)
			{
				return BTTaskStatus.FinishedWithFalse;
			}
			return BTTaskStatus.FinishedWithTrue;
		}
	}
}
