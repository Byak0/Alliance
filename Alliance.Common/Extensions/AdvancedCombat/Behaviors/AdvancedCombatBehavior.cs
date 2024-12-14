using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
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
			for (int i = 0; i < Mission.Agents.Count; i++)
			{
				Agent agent = Mission.Agents.ElementAt(i);
				if (agent == null)
				{
					continue;
				}
				List<AdvancedCombatComponent> components = agent.Components.Where(component => component is AdvancedCombatComponent).Select(component => component as AdvancedCombatComponent).ToList();
				foreach (AdvancedCombatComponent component in components)
				{
					component.OnTick(dt);
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
		}

		public void AddBoneCheckComponent(BoneCheckDuringAnimationBehavior component)
		{
			boneCheckComponents.Add(component);
		}
	}
}
