using BehaviorTreeWrapper.AbstractDecoratorsListeners;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class AlwaysTrueDecorator : BannerlordNoWaitDecorator
	{
		public AlwaysTrueDecorator() : base()
		{
		}

		public override bool Evaluate()
		{
			return true;
		}
	}
}
