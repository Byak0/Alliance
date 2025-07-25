using Alliance.Client.Extensions.AdminMenu;
using Alliance.Common.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

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
						CommonAdminMsg.SendNotificationToPeerAsServer(agent.MissionPeer.GetNetworkPeer(), "You are drowning !");
					}

					CoreUtils.TakeDamage(agent, 10);
				}
			}
		}
	}
}
