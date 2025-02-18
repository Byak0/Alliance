using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class RandomWaitDecorator : BTEventDecorator, IBTBannerlordBase
	{
		private readonly float minWaitTimeMS;
		private readonly float maxWaitTimeMS;
		private float targetTime;
		private float waitTime;

		BTBlackboardValue<Agent> _agent;
		public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

		public RandomWaitDecorator(float minWaitTimeMS, float maxWaitTimeMS) : base()
		{
			this.minWaitTimeMS = minWaitTimeMS;
			this.maxWaitTimeMS = maxWaitTimeMS;
			ResetWaitTime();
		}

		private void ResetWaitTime()
		{
			waitTime = MBRandom.RandomFloatRanged(minWaitTimeMS, maxWaitTimeMS);
			targetTime = Mission.Current?.CurrentTime ?? 0 + waitTime;
		}

		public override bool Evaluate()
		{
			if ((Mission.Current?.CurrentTime ?? 0 + waitTime) >= targetTime)
			{
				ResetWaitTime();
				return true;
			}
			return false;
		}

		public override void Notify(object[] data)
		{
		}

		public override void CreateListener()
		{
		}
	}
}
