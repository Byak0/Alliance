using Alliance.Common.Extensions.Revive.Models;
using Alliance.Common.Extensions.Revive.NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MPPerkObject;

namespace Alliance.Common.Extensions.Revive.Behaviors
{
    /// <summary>
    /// MissionBehavior used to keep track of fallen players who can still be revived.
    /// </summary>
    public class ReviveBehavior : MissionNetwork, IMissionBehavior
    {
        public event Action<WoundedAgentInfos> OnNewWounded;
        public List<WoundedAgentInfos> WoundedAgents = new List<WoundedAgentInfos>();

        public ReviveBehavior() : base()
        {
        }

        public override void OnMissionTick(float dt)
        {
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.IsHuman)
            {
                GameEntity woundedAgentEntity = CreateWoundedAgent(affectedAgent);
                WoundedAgentInfos woundedAgentInfos;

                if (affectedAgent.MissionPeer != null)
                {
                    woundedAgentInfos = CreatePlayerWoundedInfo(affectedAgent, woundedAgentEntity);
                }
                else
                {
                    woundedAgentInfos = CreateAIWoundedInfo(affectedAgent, woundedAgentEntity);
                }

                WoundedAgents.Add(woundedAgentInfos);
                OnNewWounded?.Invoke(woundedAgentInfos);

                // Hide agent
                affectedAgent.AgentVisuals.SetVisible(false);
            }
        }

        private WoundedAgentInfos CreateAIWoundedInfo(Agent affectedAgent, GameEntity woundedAgentEntity)
        {
            return new WoundedAgentInfos(
                woundedAgentEntity,
                affectedAgent.Name,
                null,
                affectedAgent.Team.Side,
                affectedAgent.Banner?.Culture,
                affectedAgent.Character,
                null,
                affectedAgent.Frame,
                affectedAgent.Formation != null ? affectedAgent.Formation.Index : 0,
                1f,
                null,
                1
            );
        }

        private WoundedAgentInfos CreatePlayerWoundedInfo(Agent affectedAgent, GameEntity woundedAgentEntity)
        {
            MissionPeer peer = affectedAgent.MissionPeer;
            MPOnSpawnPerkHandler spawnPerkHandler = GetOnSpawnPerkHandler(peer);

            return new WoundedAgentInfos(
                woundedAgentEntity,
                peer.Name,
                peer.GetNetworkPeer(),
                affectedAgent.Team.Side,
                peer.Culture,
                affectedAgent.Character,
                spawnPerkHandler,
                affectedAgent.Frame,
                affectedAgent.Formation.Index,
                1f,
                null,
                1
            );
        }

        public GameEntity CreateWoundedAgent(Agent woundedAgent)
        {
            GameEntity woundedAgentEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
            AnimationSystemData animationSystemData = woundedAgent.Monster.FillAnimationSystemData(MBActionSet.GetActionSet(woundedAgent.Monster.ActionSetCode), 1f, false);
            woundedAgentEntity.CreateSkeletonWithActionSet(ref animationSystemData);
            woundedAgentEntity.SetGlobalFrame(woundedAgent.Frame);
            woundedAgentEntity.Skeleton.SetAgentActionChannel(0, ActionIndexCache.Create("act_fall_left_front"), 0f, -0.2f, true);

            uint color1 = woundedAgent.ClothingColor1;
            uint color2 = woundedAgent.ClothingColor2;

            List<MetaMesh> metaMeshes = new List<MetaMesh>();
            for (int i = 0; i < 9; i++)
            {
                string metaMeshName = woundedAgent.SpawnEquipment[i].Item?.MultiMeshName;
                if (!string.IsNullOrEmpty(metaMeshName))
                {
                    MetaMesh mesh = MetaMesh.GetCopy(metaMeshName, true, false);
                    mesh.SetFactor1(color1);
                    mesh.SetFactor2(color2);
                    metaMeshes.Add(mesh);
                }
            }

            foreach (MetaMesh metaMesh in metaMeshes)
            {
                woundedAgentEntity.AddMultiMeshToSkeleton(metaMesh);
            }

            woundedAgentEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("head_male_a", true, false));
            woundedAgentEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("feet_male_a", true, false));
            woundedAgentEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("hands_male_a", true, false));
            woundedAgentEntity.AddMultiMeshToSkeleton(MetaMesh.GetCopy("body_male_a", true, false));

            woundedAgentEntity.Skeleton.TickActionChannels();
            woundedAgentEntity.Skeleton.TickAnimationsAndForceUpdate(0.01f, woundedAgent.Frame, true);

            Log($"Created wounded agent entity at {woundedAgent.Frame.origin}", LogLevel.Debug);

            return woundedAgentEntity;
        }

        public void RescueAgent(WoundedAgentInfos targetWoundedAgent)
        {
            if (!WoundedAgents.Contains(targetWoundedAgent))
                return;

            WoundedAgents.Remove(targetWoundedAgent);
            targetWoundedAgent.WoundedAgentEntity.Skeleton.SetAgentActionChannel(0, ActionIndexCache.Create("act_stand_up_floor_1"), 0f, -0.2f, true);
            Log("Rescuing " + targetWoundedAgent.Name, LogLevel.Debug);
            Task.Run(() => DelayedSpawnAgent(targetWoundedAgent, 4200));
        }

        private static async void DelayedSpawnAgent(WoundedAgentInfos _targetWoundedAgent, int waitTime)
        {
            await Task.Delay(waitTime);

            try
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new RequestRescueAgent(
                    _targetWoundedAgent.WoundedAgentEntity.GetGlobalFrame(),
                    true,
                    _targetWoundedAgent.Character.StringId,
                    _targetWoundedAgent.PreviousFormation,
                    _targetWoundedAgent.AgentDifficulty));
                GameNetwork.EndModuleEventAsClient();

                _targetWoundedAgent.WoundedAgentEntity.Remove(0);
            }
            catch (Exception ex)
            {
                Log($"Error rescuing agent", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}