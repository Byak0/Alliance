using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentComponents
{
	/// <summary>
	/// Default agent component for Alliance. Includes an agent navigator like in Singleplayer, but available in Multiplayer.
	/// </summary>
	public class AL_DefaultAgentComponent : AgentComponent
	{
		public AL_AgentNavigator AgentNavigator { get; private set; }

		public AL_DefaultAgentComponent(Agent agent)
			: base(agent)
		{
			AgentNavigator = new AL_AgentNavigator(Agent);
		}

		public void OnAgentRemoved(Agent agent)
		{
			AgentNavigator?.OnAgentRemoved(agent);
		}

		public virtual void OnTick(float dt)
		{
		}

		public override void OnTickAsAI(float dt)
		{
			AgentNavigator?.Tick(dt, false);
		}

		public override void OnStopUsingGameObject()
		{
			if (Agent.IsAIControlled)
			{
				AgentNavigator?.OnStopUsingGameObject();
			}
		}

		public virtual void OnMissionResultReady(MissionResult missionResult) { }
	}
}
