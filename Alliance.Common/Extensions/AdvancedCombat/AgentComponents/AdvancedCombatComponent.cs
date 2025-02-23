using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentComponents
{
	public abstract class AdvancedCombatComponent : AgentComponent
	{
		protected AdvancedCombatComponent(Agent agent) : base(agent)
		{
		}

		public abstract void OnTick(float dt);
	}
}