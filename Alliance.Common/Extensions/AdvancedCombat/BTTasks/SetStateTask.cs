using Alliance.Common.Extensions.AdvancedCombat.BTBehaviorTrees;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class SetStateTask : BTTask, IBTStateBlackboard
	{
		private readonly BTState targetState;

		BTBlackboardValue<BTState> state;
		public BTBlackboardValue<BTState> State { get => state; set => state = value; }

		public SetStateTask(BTState targetState) : base()
		{
			this.targetState = targetState;
		}

		public override BTTaskStatus Execute()
		{
			State.SetValue(targetState);
			return BTTaskStatus.FinishedWithTrue;
		}
	}
}
