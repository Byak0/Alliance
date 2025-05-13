using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System;
using System.Collections.Generic;
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

		public override BTTaskStatus Execute()
		{
			Agent agent = Agent.GetValue();
			if (agent == null || !agent.IsActive() || agent.IsFadingOut())
				return BTTaskStatus.FinishedWithFalse;

			// Play the animation
			agent.SetActionChannel(0, Action);

			// Get potential targets
			List<Agent> targets = CoreUtils.GetNearAliveAgentsInRange(TargetDetectionRange, agent).FindAll(agt => agt != agent && agt.RiderAgent != agent && agt.IsActive());
			if (targets.Count == 0) return BTTaskStatus.FinishedWithTrue;

			// Check for collisions
			AdvancedCombatBehavior advancedCombat = Mission.Current.GetMissionBehavior<AdvancedCombatBehavior>();
			advancedCombat.AddBoneCheckComponent(new BoneCheckDuringAnimation(
						Action,
						agent,
						targets,
						BoneIds,
						ActionProgressMin,
						ActionProgressMax,
						BoneCollisionRadius,
						StopOnFirstHit,
						OnCollisionCallback,
						() => { }
					));

			return BTTaskStatus.FinishedWithTrue;
		}
	}
}
