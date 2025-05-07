using BehaviorTrees;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class AlwaysTrueDecorator : AbstractDecorator
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
