using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.Behaviors
{
	/// <summary>
	/// Temporary class checking for collision between an agent and its targets. Removed after timer end.
	/// </summary>
	public class BoneCheck
	{
		protected Agent _agent;
		protected List<Agent> _targets;
		protected List<sbyte> _boneIds;
		protected float _collisionRadiusSquared;
		protected float _maxRangeForCheck;
		protected float _maxDuration;
		protected bool _stopOnFirstHit;

		protected float _boneCheckLifeTime = 0f;

		// Callback when a collision is detected
		protected Action<Agent, Agent, sbyte> _onCollisionCallback;
		// Callback when the action expires
		protected Action _onExpiration;

		public BoneCheck(Agent agent, List<Agent> targets, List<sbyte> boneIds, float maxDuration, float boneCollisionRadius, bool stopAfterFirstHit, Action<Agent, Agent, sbyte> onCollisionCallback, Action onExpiration)
		{
			_agent = agent;
			_targets = targets;
			_boneIds = boneIds;
			_collisionRadiusSquared = boneCollisionRadius * boneCollisionRadius;
			_maxRangeForCheck = Math.Max(10f, _collisionRadiusSquared * 10f);
			_maxDuration = maxDuration;
			_stopOnFirstHit = stopAfterFirstHit;
			_onCollisionCallback = onCollisionCallback;
			_onExpiration = onExpiration;
		}

		public virtual bool Tick(float dt)
		{
			_boneCheckLifeTime += dt;
			// Check if the lifetime has exceeded the maximum duration.
			if (_boneCheckLifeTime >= _maxDuration)
			{
				_onExpiration?.Invoke();
				// Remove this instance
				return false;
			}

			if (!CheckBoneCollision())
			{
				_onExpiration?.Invoke();
				// Remove this instance
				return false;
			}

			// Keep this instance alive
			return true;
		}

		/// <summary>
		/// Search for bone in range.
		/// </summary>
		/// <returns>False to indicate incorrect or expired state.</returns>
		protected bool CheckBoneCollision()
		{
			// Cache skeleton and global frame
			Skeleton agentSkeleton = _agent.AgentVisuals?.GetSkeleton();
			if (agentSkeleton == null)
			{
				Log($"Failed to get visuals or skeleton for {_agent.Name}", LogLevel.Debug);
				return false;
			}
			MatrixFrame agentGlobalFrame = _agent.AgentVisuals.GetGlobalFrame();

			// Pre-calculate the global positions for the candidate bones on the agent.
			List<(sbyte, Vec3)> agentBonePositions = new List<(sbyte, Vec3)>();
			int boneCount = agentSkeleton.GetBoneCount();
			foreach (sbyte bone in _boneIds)
			{
				if (bone < 0 || bone >= boneCount)
				{
					Log($"Invalid bone index {bone} for agent {_agent.Name}", LogLevel.Error);
					continue;
				}
				MatrixFrame agentBoneFrame = agentSkeleton.GetBoneEntitialFrameWithIndex(bone);
				Vec3 agentBoneGlobalPos = agentGlobalFrame.TransformToParent(agentBoneFrame.origin);
				agentBonePositions.Add((bone, agentBoneGlobalPos));

#if DEBUG
				// Show debug capsule for the bones.
				CapsuleData data = new CapsuleData();
				agentSkeleton.GetBoneBody(bone, ref data);
				Vec3 p1 = agentBoneGlobalPos;
				Vec3 p2 = agentBoneGlobalPos + data.P2;
				MBDebug.RenderDebugCapsule(p1, p2, _collisionRadiusSquared);
#endif
			}

			// Iterate over the targets and look for collision.
			for (int i = 0; i < _targets.Count; i++)
			{
				Agent target = _targets[i];
				Skeleton targetSkeleton = target.AgentVisuals?.GetSkeleton();
				if (targetSkeleton == null)
				{
					Log($"Failed to get visuals or skeleton for {target.Name}", LogLevel.Debug);
					_targets.RemoveAt(i);
					continue;
				}
				MatrixFrame targetGlobalFrame = target.AgentVisuals.GetGlobalFrame();
				sbyte boneId = FindBoneInRange(agentGlobalFrame, agentBonePositions, targetSkeleton, targetGlobalFrame);
				if (boneId != -1)
				{
					_targets.RemoveAt(i);
					_onCollisionCallback?.Invoke(_agent, target, boneId);
					if (_stopOnFirstHit)
					{
						// Stop checking after the first hit
						return false;
					}
				}
			}

			// Keep this instance alive
			return true;
		}

		protected sbyte FindBoneInRange(MatrixFrame agentGlobalFrame, List<(sbyte boneId, Vec3 position)> agentBonePositions, Skeleton targetSkeleton, MatrixFrame targetGlobalFrame)
		{
			int targetBoneCount = targetSkeleton.GetBoneCount();
			// Preliminary check: if the overall distance between agents is far greater than range, skip.
			if ((targetGlobalFrame.origin - agentGlobalFrame.origin).LengthSquared > _maxRangeForCheck)
			{
				return -1;
			}

			// Iterate over the target's bones once.
			for (int i = 0; i < targetBoneCount; i++)
			{
				MatrixFrame targetBoneFrame = targetSkeleton.GetBoneEntitialFrameWithIndex((sbyte)i);
				Vec3 targetBoneGlobalPos = targetGlobalFrame.TransformToParent(targetBoneFrame.origin);
				// Compare against all candidate attacker bones.
				foreach (var (boneId, agentBonePos) in agentBonePositions)
				{
					float distanceSquared = (targetBoneGlobalPos - agentBonePos).LengthSquared;
					if (distanceSquared <= _collisionRadiusSquared)
					{
						return (sbyte)i;
					}
				}
			}
			return -1;
		}
	}
}

