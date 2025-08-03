using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using Alliance.Common.Extensions.AdvancedCombat.BTBehaviorTrees;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using Alliance.Common.Extensions.AdvancedCombat.NetworkMessages.FromClient;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.Behaviors
{
	/// <summary>
	/// Handle unique combat behaviors. Ties a dedicated component to some creatures when they spawn.
	/// </summary>
	public class AdvancedCombatBehavior : MissionLogic
	{
		private DateTime _wargLastAttackTime = DateTime.MinValue;
		private bool _ridingWarg;

		// List of temporary components for bone collision checks
		private List<BoneCheck> _boneCheckComponents = new List<BoneCheck>();

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			// Register Behavior Trees
			BehaviorTrees.BTRegister.RegisterClass("TrollTree", objects => TrollBehaviorTree.BuildTree(objects));
		}

		public override void OnMissionTick(float dt)
		{
			// Only on client or singleplayer
			if (!GameNetwork.IsServer)
			{
				TickWargRider();
			}
			// Only on server or singeplayer
			if (!GameNetwork.IsClientOrReplay)
			{
				BoneCollisionChecks(dt);
				TickAgentComponents(dt);
			}
		}

		private void BoneCollisionChecks(float dt)
		{
			// Iterate through the list of components and tick each one.
			for (int i = _boneCheckComponents.Count - 1; i >= 0; i--)
			{
				bool isAlive = _boneCheckComponents[i].Tick(dt);
				if (!isAlive)
				{
					_boneCheckComponents.RemoveAt(i);
				}
			}
		}

		private void TickAgentComponents(float dt)
		{
			for (int i = 0; i < Mission.AllAgents.Count; i++)
			{
				Agent agent = Mission.AllAgents.ElementAt(i);
				if (agent == null)
				{
					continue;
				}
				List<AL_DefaultAgentComponent> components = agent.Components.Where(component => component is AL_DefaultAgentComponent).Select(component => component as AL_DefaultAgentComponent).ToList();
				float offset_Z = agent.HasMount ? 1f : 0f;
				foreach (AL_DefaultAgentComponent component in components)
				{
					component.OnTick(dt);

#if DEBUG
					// Show debug information for each agent
					if (!Input.IsKeyDown(InputKey.LeftAlt)) continue;
					MatrixFrame position = agent.AgentVisuals.GetFrame();
					position.Elevate(offset_Z);
					string debugMessage = component.GetType().Name + " | " + agent.Health;

					uint color = Color.White.ToUnsignedInteger();
					if (component is HumanoidComponent defaultHumanoidComponent)
					{
						if (defaultHumanoidComponent.Threat != null)
						{
							color = new Color(0.8f, 0.1f, 0.1f).ToUnsignedInteger();
						}
						if (defaultHumanoidComponent.Target != null)
						{
							color = new Color(1f, 0f, 0f).ToUnsignedInteger();
						}
						debugMessage += "\nThreat: " + defaultHumanoidComponent.Threat?.Name;
						debugMessage += "\nTarget: " + defaultHumanoidComponent.Target?.Name;
						debugMessage += "\nForgetThreatTimer: " + defaultHumanoidComponent.ForgetThreatTimer;
					}
					else if (component is EntComponent entComponent)
					{
						color = new Color(0f, 1f, 0f).ToUnsignedInteger();
					}
					else if (component is TrollComponent trollComponent)
					{
						color = new Color(0f, 1f, 1f).ToUnsignedInteger();
						debugMessage += "\nKickCD: " + Math.Max(0, 8 - trollComponent.KickCD);
					}
					else if (component is WargComponent wargComponent)
					{
						if (wargComponent.Threat != null)
						{
							color = new Color(0.8f, 0.1f, 0.1f).ToUnsignedInteger();
						}
						if (wargComponent.Target != null)
						{
							color = new Color(1f, 0f, 0f).ToUnsignedInteger();
						}
						debugMessage += "\nState: " + wargComponent.CurrentState;
						debugMessage += "\nTarget: " + wargComponent.Target?.Name;
						debugMessage += "\nThreat: " + wargComponent.Threat?.Name;
						debugMessage += "\nFearOfThreat: " + wargComponent.FearOfThreat;
						debugMessage += "\nLastAttacker: " + wargComponent.LastAttacker?.Name;
						debugMessage += "\nIsWounded: " + wargComponent.IsWounded;
					}
					MBDebug.RenderDebugText3D(position.origin, debugMessage, color);
#endif
				}
			}
		}

		private void TickWargRider()
		{
			if (Agent.Main != null && Agent.Main.HasMount && Agent.Main.MountAgent.IsWarg())
			{
				CheckForWargAttack();
				UpdateWargRiderHandle();
			}
			else if (_ridingWarg)
			{
				_ridingWarg = false;
				ClearCustomLookDirection();
			}
		}

		private static void ClearCustomLookDirection()
		{
			MissionMainAgentController missionMainAgentController = Mission.Current.GetMissionBehavior<MissionMainAgentController>();
			missionMainAgentController.CustomLookDir = Vec3.Zero;
		}

		private void UpdateWargRiderHandle()
		{
			_ridingWarg = true;
			MissionMainAgentController missionMainAgentController = Mission.Current.GetMissionBehavior<MissionMainAgentController>();

			// Force rider to look in front of him to prevent bad hands positions. Only when in movement and not in first person.
			if (Agent.Main.GetCurrentAction(0) == ActionIndexCache.act_none && Agent.Main.GetCurrentAction(1) == ActionIndexCache.act_none && !Agent.Main.HeadCameraMode)
			{
				Vec3 newLookDir = Agent.Main.GetMovementDirection().ToVec3();
				missionMainAgentController.CustomLookDir = newLookDir;
				return;
			}
			// Reset look direction when not in riding animation (attack, block, etc.)
			else
			{
				ClearCustomLookDirection();
			}
		}

		private void CheckForWargAttack()
		{
			// Check for user input
			if (Input.IsKeyPressed(InputKey.Q))
			{
				// Check if cooldown has passed
				if (DateTime.UtcNow - _wargLastAttackTime >= TimeSpan.FromSeconds(WargConstants.ATTACK_COOLDOWN))
				{
					Log($"Using Warg attack !", LogLevel.Information);

					if (GameNetwork.IsClient)
					{
						// Request server to perform attack
						GameNetwork.BeginModuleEventAsClient();
						GameNetwork.WriteMessage(new RequestSpecialAttack());
						GameNetwork.EndModuleEventAsClient();
					}
					else
					{
						// Perform attack locally if not in multiplayer
						Agent.Main.MountAgent.WargAttack();
					}

					_wargLastAttackTime = DateTime.UtcNow;
				}
				else
				{
					Log($"Warg attack on cooldown! {(TimeSpan.FromSeconds(WargConstants.ATTACK_COOLDOWN) - (DateTime.UtcNow - _wargLastAttackTime)).Seconds + 1} seconds remaining", LogLevel.Information);
				}
			}
		}

		public override void OnAgentCreated(Agent agent)
		{
		}

		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (GameNetwork.IsClientOrReplay) return;

			if (agent.IsWarg())
			{
				agent.AddComponent(new WargComponent(agent));
			}
			else if (agent.IsTroll())
			{
				agent.AddComponent(new HumanoidComponent(agent));
				//agent.AddComponent(new BehaviorTreeAgentComponent(agent, "TrollTree"));
				agent.AddComponent(new TrollComponent(agent));
				//Log("Added TrollComponent to agent", LogLevel.Debug);
			}
			else if (agent.IsEnt())
			{
				agent.AddComponent(new EntComponent(agent));
			}
			else if (agent.IsHuman)
			{
				agent.AddComponent(new HumanoidComponent(agent));
			}
		}

		public override void OnMissionResultReady(MissionResult missionResult)
		{
			for (int i = 0; i < Mission.AllAgents.Count; i++)
			{
				Agent agent = Mission.AllAgents.ElementAt(i);
				if (agent == null)
				{
					continue;
				}
				agent.GetComponent<AL_DefaultAgentComponent>()?.OnMissionResultReady(missionResult);
			}
		}

		public void AddBoneCheckComponent(BoneCheck component)
		{
			_boneCheckComponents.Add(component);
		}
	}
}
