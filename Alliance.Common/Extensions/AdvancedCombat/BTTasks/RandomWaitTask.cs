using BehaviorTrees;
using BehaviorTrees.Nodes;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class RandomWaitTask : BTTask
	{
		private readonly float minWaitTimeS;
		private readonly float maxWaitTimeS;
		private float targetTime;
		private float waitTime;

		public RandomWaitTask(float minWaitTimeS, float maxWaitTimeS) : base()
		{
			this.minWaitTimeS = minWaitTimeS;
			this.maxWaitTimeS = maxWaitTimeS;
			waitTime = MBRandom.RandomFloatRanged(minWaitTimeS, maxWaitTimeS);
			targetTime = (Mission.Current?.CurrentTime ?? 0) + waitTime;
		}

		private void ResetWaitTime()
		{
			waitTime = MBRandom.RandomFloatRanged(minWaitTimeS, maxWaitTimeS);
			targetTime = (Mission.Current?.CurrentTime ?? 0) + waitTime;
		}

		public override BTTaskStatus Execute()
		{
			if (Mission.Current?.CurrentTime >= targetTime)
			{
				ResetWaitTime();
				return BTTaskStatus.FinishedWithTrue;
			}
			return BTTaskStatus.FinishedWithFalse;
		}
	}
}
