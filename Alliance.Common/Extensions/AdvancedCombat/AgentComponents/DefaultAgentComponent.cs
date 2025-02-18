using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentComponents
{
	public class DefaultAgentComponent : AgentComponent
	{
		public AL_AgentNavigator AgentNavigator { get; private set; }

		public DefaultAgentComponent(Agent agent)
			: base(agent)
		{
		}

		public AL_AgentNavigator CreateAgentNavigator()
		{
			AgentNavigator = new AL_AgentNavigator(Agent);
			return AgentNavigator;
		}

		public void OnAgentRemoved(Agent agent)
		{
			AL_AgentNavigator DefaultAgentNavigator = AgentNavigator;
			if (DefaultAgentNavigator == null)
			{
				return;
			}
			DefaultAgentNavigator.OnAgentRemoved(agent);
		}

		public override void OnTickAsAI(float dt)
		{
			AL_AgentNavigator DefaultAgentNavigator = AgentNavigator;
			if (DefaultAgentNavigator == null)
			{
				return;
			}
			DefaultAgentNavigator.Tick(dt, false);
		}

		public override void OnStopUsingGameObject()
		{
			if (Agent.IsAIControlled)
			{
				AL_AgentNavigator DefaultAgentNavigator = AgentNavigator;
				if (DefaultAgentNavigator == null)
				{
					return;
				}
				DefaultAgentNavigator.OnStopUsingGameObject();
			}
		}
	}
}
