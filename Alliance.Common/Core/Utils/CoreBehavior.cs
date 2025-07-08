using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Core.Utils
{
	public class CoreBehavior : MissionBehavior
	{
		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			if (GameNetwork.IsClient) ConfigManager.Instance.SendMyConfigToServer(UserConfig.Instance);
		}

		public override void OnMissionTick(float dt)
		{
			if (Mission.Current != null)
			{
				SpatialGrid.UpdateGrid(Mission.Current.AllAgents);

#if DEBUG
				// Show nearby agents using the spatial grid
				if (Agent.Main != null && Input.IsKeyDown(InputKey.LeftAlt))
				{
					List<Agent> nearbyAllAgents = CoreUtils.GetNearAliveAgentsInRange(20f, Agent.Main);
					foreach (Agent agent in nearbyAllAgents)
					{
						if (!agent.IsActive())
							continue;
						MBDebug.RenderDebugSphere(agent.Position, 0.1f, Colors.Blue.ToUnsignedInteger());
					}
				}
#endif
			}
		}
	}
}