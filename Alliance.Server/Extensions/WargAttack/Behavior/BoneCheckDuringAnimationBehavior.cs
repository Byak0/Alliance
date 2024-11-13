using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.Extensions.WargAttack.Behavior
{
	/// <summary>
	/// Temporary class checking for collision between an agent and its target. Removed after collision or timer.
	/// </summary>
	public class BoneCheckDuringAnimationBehavior : MissionBehavior
	{
		private readonly List<sbyte> Parry1HBoneIds = new List<sbyte>() { 24, 25, 26, 27 };
		private readonly List<sbyte> Parry2HBoneIds = new List<sbyte>() { 17, 18, 19, 20, 24, 25, 26, 27 };

		private readonly List<ActionCodeType> Parry1HActionType = new List<ActionCodeType>()
			{
				ActionCodeType.DefendForward1h,
				ActionCodeType.DefendUp1h,
				ActionCodeType.DefendRight1h,
				ActionCodeType.DefendLeft1h,
			};

		private Agent agent;
		private Agent target;
		private List<sbyte> boneIds;
		private float minDistanceSquared;
		private float startDelay;
		private float duration;
		private bool isChecking;

		float time = 0f;

		// Callback to signal when a bone is found
		public Action<Agent, Agent> OnBoneFoundCallback;
		public Action<Agent, Agent> OnBoneFoundParryCallback;

		// Constructor callback
		public BoneCheckDuringAnimationBehavior(Agent agent, Agent target, List<sbyte> boneIds, float startDelay, float duration, float minDistanceSquared, Action<Agent, Agent> onBoneFoundCallback, Action<Agent, Agent> onBoneFoundParryCallback)
		{
			this.agent = agent;
			this.target = target;
			this.boneIds = boneIds;
			this.minDistanceSquared = minDistanceSquared;
			this.startDelay = startDelay;
			this.duration = duration;
			isChecking = true;
			OnBoneFoundCallback = onBoneFoundCallback;
			OnBoneFoundParryCallback = onBoneFoundParryCallback;
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);

			time += dt;

			if (!isChecking || time >= duration)
			{
				isChecking = false;
				Mission.Current.RemoveMissionBehavior(this);
				return;
			}

			if (time < startDelay) return;

			sbyte boneInRange = SearchBoneInRange(agent, target, boneIds, minDistanceSquared);

			// If a bone is found, call the callback.
			if (boneInRange != -1)
			{
				// Check if target is parrying
				ActionCodeType currentActionType = target.GetCurrentActionType(1);
				bool parrying = !target.IsMount && currentActionType > 0 && currentActionType <= ActionCodeType.DefendLeftStaff;
				bool parryingWithRightHand = Parry1HActionType.Contains(currentActionType);
				bool parryingWithBothHands = parrying && !parryingWithRightHand;
				// Check if correct bone collided depending on parry type
				if (parryingWithRightHand && Parry1HBoneIds.Contains(boneInRange)
					|| parryingWithBothHands && Parry2HBoneIds.Contains(boneInRange))
				{
					Log($"{target.Name} successfully parry attack", LogLevel.Debug);
					OnBoneFoundParryCallback?.Invoke(agent, target); // Execute parry callback
				}
				else // No parry or parry failed
				{
					OnBoneFoundCallback?.Invoke(agent, target); // Execute standard callback
				}
				isChecking = false; // Stop the check after executing the action.
				Mission.Current.RemoveMissionBehavior(this);
				return;
			}
		}

		/// <summary>
		/// Search for bone in range
		/// </summary>
		/// <param name="agent">player</param>
		/// <param name="target">Target</param>
		/// <param name="boneType">Bone type of the player to check the collision</param>
		/// <param name="rangeSquared"></param>
		/// <returns>-1 if no target bone in range else the first target bone id in range</returns>
		private static sbyte SearchBoneInRange(Agent agent, Agent target, List<sbyte> boneType, float rangeSquared)
		{
			int targetBoneCount = target.AgentVisuals.GetSkeleton().GetBoneCount();

			foreach (sbyte bone in boneType)
			{
				MatrixFrame agentGlobalFrame = agent.AgentVisuals.GetGlobalFrame();
				MatrixFrame targetGlobalFrame = target.AgentVisuals.GetGlobalFrame();
				MatrixFrame agentBone = agent.AgentVisuals.GetSkeleton().GetBoneEntitialFrameWithIndex(bone);

				Vec3 agentBoneGlobalPosition = agentGlobalFrame.TransformToParent(agentBone.origin);

				sbyte closestBone = -1;

				for (int i = 0; i < targetBoneCount; i++)
				{
					MatrixFrame targetBoneFrame = target.AgentVisuals.GetSkeleton().GetBoneEntitialFrameWithIndex((sbyte)i);
					Vec3 targetBoneGlobalPosition = targetGlobalFrame.TransformToParent(targetBoneFrame.origin);

					float distanceSquared = (targetBoneGlobalPosition - agentBoneGlobalPosition).LengthSquared;

					if (distanceSquared <= rangeSquared)
					{
						closestBone = (sbyte)i;
						return closestBone;
					}
				}
			}
			return -1; // no target found in range
		}

		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
	}
}

