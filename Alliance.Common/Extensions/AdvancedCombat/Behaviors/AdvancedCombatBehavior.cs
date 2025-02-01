using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.Behaviors
{
	/// <summary>
	/// Ties a dedicated component to some creatures when they spawn, to unlock advanced and unique behaviors.
	/// </summary>
	public class AdvancedCombatBehavior : MissionLogic
	{
		// List of temporary components for bone collision checks
		private List<BoneCheckDuringAnimationBehavior> boneCheckComponents = new List<BoneCheckDuringAnimationBehavior>();

		public override void OnMissionTick(float dt)
		{
			// Iterate through the list of components and tick each one.
			for (int i = boneCheckComponents.Count - 1; i >= 0; i--)
			{
				bool isAlive = boneCheckComponents[i].Tick(dt);
				if (!isAlive)
				{
					boneCheckComponents.RemoveAt(i);
				}
			}

			// TODO Rework iteration to prevent crash when list gets modified
			for (int i = 0; i < Mission.AllAgents.Count; i++)
			{
				Agent agent = Mission.AllAgents.ElementAt(i);
				if (agent == null)
				{
					continue;
				}
				List<AdvancedCombatComponent> components = agent.Components.Where(component => component is AdvancedCombatComponent).Select(component => component as AdvancedCombatComponent).ToList();
				float offset_Z = agent.HasMount ? 1f : 0f;
				foreach (AdvancedCombatComponent component in components)
				{
					component.OnTick(dt);

#if DEBUG
					if (!Input.IsKeyDown(InputKey.LeftAlt)) continue;
					MatrixFrame position = agent.AgentVisuals.GetFrame();
					position.Elevate(offset_Z);
					string debugMessage = component.GetType().Name + " | " + agent.Health;
					debugMessage += "\nChannel 0: " + agent.GetCurrentAction(0)?.Name;
					debugMessage += "\nChannel 1: " + agent.GetCurrentAction(1)?.Name;

					uint color = Color.White.ToUnsignedInteger();
					if (component is DefaultHumanoidComponent defaultHumanoidComponent)
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

		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (agent.IsWarg())
			{
				agent.AddComponent(new WargComponent(agent));
				//Log("Added WargComponent to agent", LogLevel.Debug);
			}
			else if (agent.IsTroll())
			{
				agent.AddComponent(new TrollComponent(agent));
				//Log("Added TrollComponent to agent", LogLevel.Debug);
			}
			else if (agent.IsEnt())
			{
				agent.AddComponent(new EntComponent(agent));
			}
			else if (agent.IsHuman)
			{
				agent.AddComponent(new DefaultHumanoidComponent(agent));
			}
		}

		public void AddBoneCheckComponent(BoneCheckDuringAnimationBehavior component)
		{
			boneCheckComponents.Add(component);
		}
	}
}
