using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class RandomWaitDecorator : AbstractDecorator, IBTBannerlordBase
	{
		private readonly float minWaitTimeS;
		private readonly float maxWaitTimeS;
		private float targetTime;
		private float waitTime;

		BTBlackboardValue<Agent> _agent;
		public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

		public RandomWaitDecorator(float minWaitTimeS, float maxWaitTimeS) : base()
		{
			this.minWaitTimeS = minWaitTimeS;
			this.maxWaitTimeS = maxWaitTimeS;
			ResetWaitTime();
		}

		private void ResetWaitTime()
		{
			waitTime = MBRandom.RandomFloatRanged(minWaitTimeS, maxWaitTimeS);
			targetTime = (Mission.Current?.CurrentTime ?? 0) + waitTime;
		}

		public override bool Evaluate()
		{
			if (Mission.Current?.CurrentTime >= targetTime)
			{
				ResetWaitTime();
				return true;
			}
			return false;
		}
	}
}
