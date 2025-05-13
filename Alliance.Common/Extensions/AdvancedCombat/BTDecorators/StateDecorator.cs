using Alliance.Common.Extensions.AdvancedCombat.BTBehaviorTrees;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class StateDecorator : BannerlordNoWaitDecorator, IBTStateBlackboard
	{
		private readonly BTState targetState;

		public StateDecorator(BTState targetState) : base()
		{
			this.targetState = targetState;
		}

		BTBlackboardValue<BTState> state;
		public BTBlackboardValue<BTState> State { get => state; set => state = value; }

		public override bool Evaluate()
		{
			return targetState == State.GetValue();
		}
	}
}
