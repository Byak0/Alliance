using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class PlayAnimationAndCheckCollisionTask : BTTask, IBTBannerlordBase
	{
		protected ActionIndexCache Action;
		protected List<sbyte> BoneIds;
		protected float TargetDetectionRange;
		protected float BoneCollisionRadius;
		protected float ActionProgressMin;
		protected float ActionProgressMax;
		protected bool StopOnFirstHit;
		protected Action<Agent, Agent, sbyte> OnCollisionCallback;

		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		public PlayAnimationAndCheckCollisionTask(ActionIndexCache action, List<sbyte> boneIds, float targetDetectionRange, float boneCollisionRadius, float actionProgressMin, float actionProgressMax, bool stopOnFirstHit, Action<Agent, Agent, sbyte> onCollisionCallback)
		{
			Action = action;
			BoneIds = boneIds;
			TargetDetectionRange = targetDetectionRange;
			BoneCollisionRadius = boneCollisionRadius;
			ActionProgressMin = actionProgressMin;
			ActionProgressMax = actionProgressMax;
			StopOnFirstHit = stopOnFirstHit;
			OnCollisionCallback = onCollisionCallback;
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Agent agent = Agent.GetValue();
			if (agent == null || !agent.IsActive() || agent.IsFadingOut())
				return false;

			agent.CustomAttack(Action, BoneIds, ActionProgressMin, ActionProgressMax, TargetDetectionRange, BoneCollisionRadius, StopOnFirstHit, OnCollisionCallback);

			return true;
		}
	}
}
