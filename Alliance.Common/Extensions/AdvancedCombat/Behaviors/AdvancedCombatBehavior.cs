using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.Behaviors
{
	/// <summary>
	/// Handle unique combat behaviors. Ties a dedicated component to some creatures when they spawn.
	/// </summary>
	public class AdvancedCombatBehavior : MissionLogic
	{
		// List of temporary components for bone collision checks
		private List<BoneCheck> _boneCheckComponents = new List<BoneCheck>();

		public override void OnMissionTick(float dt)
		{
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
					MBDebug.RenderDebugText3D(position.origin, debugMessage, color);
#endif
				}
			}
		}

		public override void OnAgentCreated(Agent agent)
		{
		}

		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (GameNetwork.IsClientOrReplay) return;

			if (agent.IsHuman)
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
