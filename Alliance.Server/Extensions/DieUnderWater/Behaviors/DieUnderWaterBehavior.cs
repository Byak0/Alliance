using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.Extensions.DieUnderWater.Behaviors
{
    public class DieUnderWaterBehavior : MissionNetwork, IMissionBehavior
    {
        private float waterLevel;
        private List<Agent> allAgentList = new();
        private DieAgentManager agentManager = new();
        private MultiplayerRoundController roundController;

        public override void AfterStart()
        {
            base.AfterStart();
            waterLevel = Mission.Current.GetWaterLevelAtPosition(Vec2.Zero);
            Log("Water level is " + waterLevel, LogLevel.Information);
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();

            if (roundController != null)
            {
                roundController.OnRoundStarted += UpdateAllAgentList;
                roundController.OnRoundEnding += RemoveAgentManager;
            }
            else
            {
                UpdateAllAgentList();
            }
        }
        public override void OnRemoveBehavior()
        {
            if (roundController != null)
            {
                roundController.OnRoundStarted -= UpdateAllAgentList;
                roundController.OnRoundEnding -= RemoveAgentManager;
            }
            else
            {
                RemoveAgentManager();
            }
            base.OnRemoveBehavior();
        }

        public void RemoveAgentManager()
        {
            agentManager = null;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            //Do nothing in case of not init
            if (agentManager == null || allAgentList.Count == 0) return;

            foreach (Agent agent in allAgentList)
            {
                if (IsAgentEligible(agent))
                {
                    if (agent.GetEyeGlobalPosition().Z < waterLevel)
                    {
                        agentManager.SendDeathSignal(agent);
                    }
                    else
                    {
                        agentManager.SendStopDeathSignal(agent);
                    }
                }
            }

            agentManager.HandleDamageOnAgentsOnTick();
        }

        private static bool IsAgentEligible(Agent agent)
        {
            return agent != null && agent.Health >= 0;
        }

        /// <summary>
        /// Will init AllAgents list after X seconds.
        /// This is use as optimisation.
        /// </summary>
        private async void UpdateAllAgentList()
        {
            await Task.Delay(15000);

            if (Mission.Current == null || Mission.Current.AllAgents == null) return;
            if (agentManager == null) agentManager = new DieAgentManager();

            allAgentList = Mission.Current.AllAgents;
            agentManager.UpdateAgentDieDico(allAgentList);

            UpdateAllAgentList();
        }

        private class DieAgentManager
        {
            // Contain agent as key and DieAgentModel indicating if agent need to die or not.
            private Dictionary<Agent, DieAgentModel> agentDieDico = new();

            public DieAgentManager() { }

            public void UpdateAgentDieDico(List<Agent> allAgentList)
            {
                foreach (var agent in allAgentList)
                {
                    if (agentDieDico.ContainsKey(agent)) return;
                    agentDieDico.Add(agent, new DieAgentModel(agent));
                }
            }

            /// <summary>
            /// Need to set boolean of agent to true indicating that agent need to die
            /// </summary>
            /// <param name="agent"></param>
            internal void SendDeathSignal(Agent agent)
            {
                if (agentDieDico.TryGetValue(agent, out DieAgentModel dieAgentModel))
                {
                    dieAgentModel.NeedToDie = true;
                }
            }

            /// <summary>
            /// Agent need to stop dying
            /// </summary>
            /// <param name="agent"></param>
            internal void SendStopDeathSignal(Agent agent)
            {
                if (agentDieDico.TryGetValue(agent, out DieAgentModel dieAgentModel))
                {
                    dieAgentModel.NeedToDie = false;
                }
                else
                {
                    // Add missing found agent
                    agentDieDico.Add(agent, new DieAgentModel(agent));
                }
            }

            internal void HandleDamageOnAgentsOnTick()
            {
                foreach (var agent in agentDieDico)
                {
                    if (agent.Value.NeedToDie && agent.Value.LastTimeSinceEnteredInWater < (DateTime.Now - TimeSpan.FromSeconds(5)))
                    {
                        //If agent need to die and the time since agent enter water is greater than 5 sec
                        if (IsAgentEligible(agent.Key) && agent.Value.IsReadyToTakeDmg)
                        {
                            agent.Value.TakeDamage();
                        }
                    }
                }
            }

            private class DieAgentModel
            {
                private static readonly double DAMAGE_COOLDOWN_IN_SECOND = 5;

                private Agent agent;

                /// <summary>
                /// Indicate if agent need to die
                /// </summary>
                private bool needToDie = false;

                /// <summary>
                /// Indicate last time since agent entered in water (Will equals MAX value in case agent do not need to die) 
                /// </summary>
                private DateTime lastTimeSinceEnteredInWater = DateTime.MaxValue;

                /// <summary>
                /// Represent the time since last time a damage as been applied to the agent.
                /// </summary>
                private DateTime cooldownToApplyDmg = DateTime.Now;

                public bool IsReadyToTakeDmg
                {
                    get
                    {
                        bool test = DateTime.Now > (CooldownToApplyDmg + TimeSpan.FromSeconds(DAMAGE_COOLDOWN_IN_SECOND));
                        return test;
                    }
                }

                public DateTime LastTimeSinceEnteredInWater
                {
                    get => lastTimeSinceEnteredInWater;
                    private set
                    {
                        lastTimeSinceEnteredInWater = value;
                    }
                }
                public bool NeedToDie
                {
                    get => needToDie;
                    set
                    {
                        if (needToDie != value)
                        {
                            needToDie = value;

                            if (value)
                            {
                                LastTimeSinceEnteredInWater = DateTime.Now;
                            }
                            else
                            {
                                LastTimeSinceEnteredInWater = DateTime.MaxValue;
                            }
                        }
                    }
                }

                public Agent Agent { get => agent; set => agent = value; }

                public DateTime CooldownToApplyDmg { get => cooldownToApplyDmg; set => cooldownToApplyDmg = value; }

                public DieAgentModel(Agent agent)
                {
                    Agent = agent;
                }

                public void TakeDamage()
                {
                    CooldownToApplyDmg = DateTime.Now;

                    if (agent == null || agent.Health <= 0) return;

                    if (agent.MissionPeer != null)
                    {
                        GameNetwork.BeginModuleEventAsServer(agent.MissionPeer.GetNetworkPeer());
                        GameNetwork.WriteMessage(new SendNotification("You are drowning !", 0));
                        GameNetwork.EndModuleEventAsServer();
                    }

                    //TODO extract me and add me into a static Utils method

                    Blow blow = new Blow(agent.Index);
                    blow.DamageType = DamageTypes.Pierce;
                    blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                    blow.GlobalPosition = agent.Position;
                    blow.GlobalPosition.z = blow.GlobalPosition.z + agent.GetEyeGlobalHeight();
                    blow.BaseMagnitude = 50f;
                    blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                    blow.InflictedDamage = 10;
                    blow.SwingDirection = agent.LookDirection;
                    MatrixFrame frame = agent.Frame;
                    blow.SwingDirection = frame.rotation.TransformToParent(new Vec3(-1f, 0f, 0f, -1f));
                    blow.SwingDirection.Normalize();
                    blow.Direction = blow.SwingDirection;
                    blow.DamageCalculated = true;
                    sbyte mainHandItemBoneIndex = agent.Monster.MainHandItemBoneIndex;
                    AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(false, false, false, true, false, false, false, false, false, false, false, false, CombatCollisionResult.StrikeAgent, -1, 0, 2, blow.BoneIndex, BoneBodyPartType.Head, mainHandItemBoneIndex, UsageDirection.AttackLeft, -1, CombatHitResultFlags.NormalHit, 0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, Vec3.Up, blow.Direction, blow.GlobalPosition, Vec3.Zero, Vec3.Zero, agent.Velocity, Vec3.Up);
                    agent.RegisterBlow(blow, attackCollisionDataForDebugPurpose);
                }
            }
        }
    }
}
