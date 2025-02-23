using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class PlayAnimationAndCheckCollisionTask : BTTask, IBTBannerlordBase
	{
		protected ActionIndexCache Action;
		protected List<sbyte> BoneIds;
		protected float Distance;
		protected float Duration;
		protected float StartDelay;

		public PlayAnimationAndCheckCollisionTask(ActionIndexCache action, List<sbyte> boneIds, float distance, float duration, float startDelay = 0f)
		{
			Action = action;
			BoneIds = boneIds;
			Distance = distance;
			Duration = duration;
			StartDelay = startDelay;
		}

		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Agent currentAgent = Agent.GetValue();

			// Play the animation.
			currentAgent.SetActionChannel(0, Action, true);

			// Wait for the optional start delay before beginning collision checks.
			float dt = 0.03f; // time step
			float time = 0f;
			while (time < StartDelay)
			{
				if (cancellationToken.IsCancellationRequested)
					return false;
				await Task.Delay(TimeSpan.FromSeconds(dt), cancellationToken);
				time += dt;
			}

			// Check for agents within a larger radius to account for the bone's offset.
			float agentDistanceCheck = Distance * 3f;

			// Repeatedly check for collisions during the duration.
			time = 0f;
			bool hitDetected = false;
			while (time < Duration)
			{
				if (cancellationToken.IsCancellationRequested)
					return false;

				// Retrieve all nearby alive agents around the current agent.
				List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(agentDistanceCheck, currentAgent);
				foreach (Agent target in nearbyAgents)
				{
					// For each candidate, check if any of the agent's specified bones is close enough.
					sbyte boneHit = SearchBoneInRange(currentAgent, target, BoneIds, Distance);
					if (boneHit != -1)
					{
						// Collision detected: trigger an effect (for example, inflict damage).
						OnCollision(currentAgent, target, boneHit, 100);
						hitDetected = true;
						break;
					}
				}
				// TODO dont break here, continue checking for other agents but only trigger the effect once on each agent
				if (hitDetected)
					break;

				await Task.Delay(TimeSpan.FromSeconds(dt), cancellationToken);
				time += dt;
			}

			return hitDetected;
		}

		public virtual void OnCollision(Agent culprit, Agent victim, sbyte boneId, float collisionStrength)
		{
			// Placeholder for collision effect (e.g., damage, knockback, etc.)
			CoreUtils.TakeDamage(victim, culprit, 0);
		}

		/// <summary>
		/// Checks if any of the specified bones on the attacker is close enough to any bone on the target.
		/// Returns the candidate bone ID if a collision is detected, or -1 otherwise.
		/// </summary>
		/// <param name="agent">The agent performing the animation.</param>
		/// <param name="target">A candidate victim agent.</param>
		/// <param name="boneIds">List of bone IDs on the agent to check.</param>
		/// <param name="distance">Collision range.</param>
		/// <returns>The target bone ID if a collision is found; otherwise, -1.</returns>
		private sbyte SearchBoneInRange(Agent agent, Agent target, List<sbyte> boneIds, float distance)
		{
			Skeleton targetSkeleton = target.AgentVisuals?.GetSkeleton();
			Skeleton agentSkeleton = agent.AgentVisuals?.GetSkeleton();
			if (targetSkeleton == null || agentSkeleton == null)
			{
				Log($"Failed to get skeleton for {agent.Name} or {target.Name}", LogLevel.Error);
				return -1;
			}

			int targetBoneCount = targetSkeleton.GetBoneCount();
			MatrixFrame agentGlobalFrame = agent.AgentVisuals.GetGlobalFrame();
			MatrixFrame targetGlobalFrame = target.AgentVisuals.GetGlobalFrame();

			foreach (sbyte bone in boneIds)
			{
				MatrixFrame agentBoneFrame = agentSkeleton.GetBoneEntitialFrameWithIndex(bone);
				Vec3 agentBoneGlobalPosition = agentGlobalFrame.TransformToParent(agentBoneFrame.origin);

				for (int i = 0; i < targetBoneCount; i++)
				{
					MatrixFrame targetBoneFrame = targetSkeleton.GetBoneEntitialFrameWithIndex((sbyte)i);
					Vec3 targetBoneGlobalPosition = targetGlobalFrame.TransformToParent(targetBoneFrame.origin);
					float bonesDistance = (targetBoneGlobalPosition - agentBoneGlobalPosition).Length;

					if (bonesDistance <= distance)
					{
						return (sbyte)i;
					}
				}
			}
			return -1;
		}
	}





	public class AnimationBoneCheckTask : BTTask, IBTBannerlordBase
	{
		private readonly Animation animation;
		private readonly float duration; // How long to check for collisions (in seconds)
		private readonly List<sbyte> boneIds; // Bones to check on the agent
		private readonly float minDistanceSquared; // Collision threshold (squared)
		private readonly float startDelay; // Delay before collision checks begin

		// Blackboard value for the agent performing the animation
		public BTBlackboardValue<Agent> Agent { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="animation">Animation to play.</param>
		/// <param name="duration">Duration (in seconds) to check for collisions.</param>
		/// <param name="boneIds">List of bone IDs to check against.</param>
		/// <param name="minDistanceSquared">The square of the minimum collision distance.</param>
		/// <param name="startDelay">Optional delay before collision checks begin.</param>
		public AnimationBoneCheckTask(Animation animation, float duration, List<sbyte> boneIds, float minDistanceSquared, float startDelay = 0f) : base()
		{
			this.animation = animation;
			this.duration = duration;
			this.boneIds = boneIds;
			this.minDistanceSquared = minDistanceSquared;
			this.startDelay = startDelay;
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Agent currentAgent = Agent.GetValue();
			if (currentAgent == null)
			{
				Log("Agent not set", LogLevel.Error);
				return false;
			}

			// Play the animation.
			AnimationSystem.Instance.PlayAnimation(currentAgent, animation);

			// Wait for the start delay before beginning collision checks.
			float dt = 0.03f; // time step (adjust if needed)
			float time = 0f;
			while (time < startDelay)
			{
				if (cancellationToken.IsCancellationRequested)
					return false;
				await Task.Delay(TimeSpan.FromSeconds(dt), cancellationToken);
				time += dt;
			}

			// Compute the query radius from the collision threshold (minDistanceSquared).
			float queryRadius = minDistanceSquared * 5f;

			// Repeatedly check for collisions during the duration.
			time = 0f;
			bool hitDetected = false;
			while (time < duration)
			{
				if (cancellationToken.IsCancellationRequested)
					return false;

				// Retrieve all nearby alive agents around the current agent.
				List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(queryRadius, currentAgent);
				foreach (Agent candidate in nearbyAgents)
				{
					// For each candidate, check if any of the agent's specified bones is close enough.
					sbyte boneHit = SearchBoneInRange(currentAgent, candidate, boneIds, minDistanceSquared);
					if (boneHit != -1)
					{
						// Collision detected: trigger an effect (for example, inflict damage).
						CoreUtils.TakeDamage(candidate, currentAgent, 100);
						hitDetected = true;
						break;
					}
				}
				if (hitDetected)
					break;

				await Task.Delay(TimeSpan.FromSeconds(dt), cancellationToken);
				time += dt;
			}

			return hitDetected;
		}

		/// <summary>
		/// Checks if any of the specified bones on the attacker is close enough to any bone on the candidate.
		/// Returns the candidate bone ID if a collision is detected, or -1 otherwise.
		/// </summary>
		/// <param name="agent">The agent performing the animation.</param>
		/// <param name="target">A candidate enemy agent.</param>
		/// <param name="boneIds">List of bone IDs on the agent to check.</param>
		/// <param name="rangeSquared">Collision range squared.</param>
		/// <returns>The target bone ID if a collision is found; otherwise, -1.</returns>
		private sbyte SearchBoneInRange(Agent agent, Agent target, List<sbyte> boneIds, float rangeSquared)
		{
			Skeleton targetSkeleton = target.AgentVisuals?.GetSkeleton();
			Skeleton agentSkeleton = agent.AgentVisuals?.GetSkeleton();
			if (targetSkeleton == null || agentSkeleton == null)
			{
				Log($"Failed to get skeleton for {agent.Name} or {target.Name}", LogLevel.Error);
				return -1;
			}

			int targetBoneCount = targetSkeleton.GetBoneCount();
			MatrixFrame agentGlobalFrame = agent.AgentVisuals.GetGlobalFrame();
			MatrixFrame targetGlobalFrame = target.AgentVisuals.GetGlobalFrame();

			foreach (sbyte bone in boneIds)
			{
				MatrixFrame agentBoneFrame = agentSkeleton.GetBoneEntitialFrameWithIndex(bone);
				Vec3 agentBoneGlobalPosition = agentGlobalFrame.TransformToParent(agentBoneFrame.origin);

				for (int i = 0; i < targetBoneCount; i++)
				{
					MatrixFrame targetBoneFrame = targetSkeleton.GetBoneEntitialFrameWithIndex((sbyte)i);
					Vec3 targetBoneGlobalPosition = targetGlobalFrame.TransformToParent(targetBoneFrame.origin);
					float distanceSquared = (targetBoneGlobalPosition - agentBoneGlobalPosition).LengthSquared;

					if (distanceSquared <= rangeSquared)
					{
						return (sbyte)i;
					}
				}
			}
			return -1;
		}
	}
}
