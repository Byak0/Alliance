using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class AnimationTask : BTTask, IBTBannerlordBase
	{
		private readonly List<Animation> animations;

		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		public AnimationTask(List<Animation> animations) : base()
		{
			this.animations = animations;
		}

		public AnimationTask(Animation animation) : base()
		{
			animations = new List<Animation> { animation };
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			AnimationSystem.Instance.PlayAnimation(Agent.GetValue(), animations.GetRandomElement());
			return true;
		}
	}
}
