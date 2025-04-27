using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

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

			// Play the animation
			agent.SetActionChannel(0, Action);

			// Gather initial target list
			List<Agent> targets = CoreUtils.GetNearAliveAgentsInRange(TargetDetectionRange, agent).FindAll(a => a != agent && a.RiderAgent != agent && a.IsActive());
			if (targets.Count == 0)
				return true;

			// TCS to signal success/failure
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

			// Start collision detection
			AdvancedCombatBehavior behavior = Mission.Current.GetMissionBehavior<AdvancedCombatBehavior>();
			behavior.AddBoneCheckComponent(new BoneCheckDuringAnimation(
				Action, agent, targets, BoneIds,
				ActionProgressMin, ActionProgressMax,
				BoneCollisionRadius,
				StopOnFirstHit,
				onCollisionCallback: (src, tgt, boneId) =>
				{
					try
					{
						OnCollisionCallback?.Invoke(src, tgt, boneId);
					}
					catch (Exception ex)
					{
						Log("Exception in callback of PlayAnimationAndCheckCollisionTask : " + ex.ToString(), LogLevel.Error);
					}
					tcs.TrySetResult(true);
				},
				onExpiration: () =>
				{
					tcs.TrySetResult(true);
				}));

			// Wait here until either callback fires			
			return await tcs.Task;
		}
	}
}
