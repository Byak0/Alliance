using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.Behaviors
{
	/// <summary>
	/// Temporary class checking for collision between an agent and its targets. Removed after collision or timer.
	/// </summary>
	public class BoneCheckDuringAnimation
	{
		private ActionIndexCache _action;
		private Agent _agent;
		private List<Agent> _targets;
		private List<sbyte> _boneIds;
		private float _collisionRadiusSquared;
		private float _maxRangeForCheck;
		private float _actionProgressMin;
		private float _actionProgressMax;

		// Callback when a collision is detected
		private Action<Agent, Agent, sbyte> _onCollisionCallback;

		// Constructor callback
		public BoneCheckDuringAnimation(ActionIndexCache action, Agent agent, List<Agent> targets, List<sbyte> boneIds, float actionProgressMin, float actionProgressMax, float boneCollisionRadius, Action<Agent, Agent, sbyte> onCollisionCallback)
		{
			_action = action;
			_agent = agent;
			_targets = targets;
			_boneIds = boneIds;
			_collisionRadiusSquared = boneCollisionRadius * boneCollisionRadius;
			_maxRangeForCheck = Math.Max(10f, _collisionRadiusSquared * 10f);
			_actionProgressMin = actionProgressMin;
			_actionProgressMax = actionProgressMax;
			_onCollisionCallback = onCollisionCallback;
		}

		public bool Tick(float dt)
		{
			// Check current action progress
			if (_agent.GetCurrentAction(0) != _action) return false;
			if (_agent.GetCurrentActionProgress(0) >= _actionProgressMax) return false;

			if (_agent.GetCurrentActionProgress(0) >= _actionProgressMin)
			{
				CheckBoneCollision();
			}

			return true;
		}

		/// <summary>
		/// Search for bone in range.
		/// </summary>
		private void CheckBoneCollision()
		{
			// Cache skeletons and global frames once per call.
			Skeleton agentSkeleton = _agent.AgentVisuals?.GetSkeleton();
			if (agentSkeleton == null)
			{
				Log($"Failed to get visuals or skeleton for {_agent.Name}", LogLevel.Error);
				return;
			}
			MatrixFrame agentGlobalFrame = _agent.AgentVisuals.GetGlobalFrame();

			// Pre-calculate the global positions for the candidate bones on the agent.
			var agentBonePositions = new List<(sbyte boneId, Vec3 position)>(_boneIds.Count);
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

			// Iterate over the targets.
			for (int i = 0; i < _targets.Count; i++)
			{
				Agent target = _targets[i];
				Skeleton targetSkeleton = target.AgentVisuals?.GetSkeleton();
				if (targetSkeleton == null)
				{
					Log($"Failed to get visuals or skeleton for {target.Name}", LogLevel.Error);
					_targets.RemoveAt(i);
					continue;
				}
				MatrixFrame targetGlobalFrame = target.AgentVisuals.GetGlobalFrame();
				sbyte boneId = FindBoneInRange(agentGlobalFrame, agentBonePositions, targetSkeleton, targetGlobalFrame);
				if (boneId != -1)
				{
					_targets.RemoveAt(i);
					_onCollisionCallback?.Invoke(_agent, target, boneId);
				}
			}
		}

		private sbyte FindBoneInRange(MatrixFrame agentGlobalFrame, List<(sbyte boneId, Vec3 position)> agentBonePositions, Skeleton targetSkeleton, MatrixFrame targetGlobalFrame)
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

