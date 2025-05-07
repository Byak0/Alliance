using BehaviorTrees;
using TaleWorlds.Core;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class RandomChanceDecorator : AbstractDecorator
	{
		private readonly float _probability;

		/// <param name="probability">Range 0-1</param>
		public RandomChanceDecorator(float probability) : base()
		{
			_probability = probability;
		}

		public override bool Evaluate()
		{
			return MBRandom.RandomFloat <= _probability;
		}
	}
}
