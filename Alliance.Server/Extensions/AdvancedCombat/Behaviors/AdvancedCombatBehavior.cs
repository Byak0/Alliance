using Alliance.Common.Core.Utils;
using Alliance.Server.Extensions.AdvancedCombat.AgentComponents;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.AdvancedCombat.Behavior
{
	/// <summary>
	/// Ties a dedicated component to some creatures when they spawn, to unlock advanced and unique behaviors.
	/// </summary>
	public class AdvancedCombatBehavior : MissionNetwork, IMissionBehavior
	{
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (agent.IsWarg())
			{
				agent.AddComponent(new WargComponent(agent));
				Log("Added WargComponent to agent", LogLevel.Debug);
			}
			else if (agent.IsTroll())
			{
				agent.AddComponent(new TrollComponent(agent));
				Log("Added TrollComponent to agent", LogLevel.Debug);
			}
		}
	}
}
