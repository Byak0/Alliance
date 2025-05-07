using Alliance.Common.Core.Utils;
using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class EnemiesTooCloseDecorator : AbstractDecorator, IBTBannerlordBase
	{
		BTBlackboardValue<Agent> _agent;
		public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

		public EnemiesTooCloseDecorator() : base()
		{
		}

		public override bool Evaluate()
		{
			Agent agent = Agent.GetValue();

			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(2.5f, Agent.GetValue().Position).FindAll(agt =>
				!agt.IsTroll() && !agt.IsEnt() && agt.IsInFrontCone(agent, 90f));

			return nearbyAgents.Count > 0;
		}
	}
}
