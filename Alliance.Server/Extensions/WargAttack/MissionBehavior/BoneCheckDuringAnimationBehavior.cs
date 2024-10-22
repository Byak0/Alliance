using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.WargAttack.MissionBehavior
{
    public class BoneCheckDuringAnimationBehavior : TaleWorlds.MountAndBlade.MissionBehavior
    {
        private Agent agent;
        private Agent target;
        private List<sbyte> boneIds;
        private float minDistanceSquared;
        private float startTime;
        private float endTime;
        private float duration;
        private bool isChecking;

        float time = 0f;

        // Callback to signal when a bone is found
        public Action<Agent, Agent> OnBoneFoundCallback;

        // Constructor callback
        public BoneCheckDuringAnimationBehavior(Agent agent, Agent target, List<sbyte> boneIds, float duration, float minDistanceSquared, Action<Agent, Agent> onBoneFoundCallback)
        {
            this.agent = agent;
            this.target = target;
            this.boneIds = boneIds;
            this.minDistanceSquared = minDistanceSquared;
            this.startTime = Mission.Current.CurrentTime;
            this.endTime = this.startTime + duration;
            this.duration = duration;
            this.isChecking = true;
            this.OnBoneFoundCallback = onBoneFoundCallback; // register callback
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            time += dt;            
            // Check duration
            if (!isChecking || time >= duration)
            {                
                isChecking = false;
                Mission.Current.RemoveMissionBehavior(this);
                return;
            }

            // Check if the agent or the target is inactive
            if (agent == null || target == null || !agent.IsActive() || !target.IsActive())
            {
                //Common.Utilities.Logger.SendMessageToAll("Agent ou cible invalide.");
                isChecking = false;
                Mission.Current.RemoveMissionBehavior(this);
                return;
            }

            // search bone in range
            sbyte boneInRange = SearchBoneInRange(agent, target, boneIds, minDistanceSquared);

            if (boneInRange != -1)
            {                
                // If a bone is found, call the callback.
                OnBoneFoundCallback?.Invoke(agent, target); // Execute the callback
                isChecking = false; // Stop the check after executing the action.
                Mission.Current.RemoveMissionBehavior(this);
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

