using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class AnimationTask : BTTask, IBTBannerlordBase
	{
		private readonly List<ActionIndexCache> animations;

		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		public AnimationTask(List<ActionIndexCache> animations) : base()
		{
			this.animations = animations;
		}

		public AnimationTask(ActionIndexCache animation) : base()
		{
			animations = new List<ActionIndexCache> { animation };
		}

		public override BTTaskStatus Execute()
		{
			ActionIndexCache action = animations.GetRandomElement();
			Agent agent = Agent.GetValue();
			agent?.SetActionChannel(0, action);
			return BTTaskStatus.FinishedWithTrue;
		}
	}
}
