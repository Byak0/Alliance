using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class MoveToRandomPlaceTask : BTTask, IBTMobile, IBTBannerlordBase
	{
		private readonly float maxDistance;

		BTBlackboardValue<AL_AgentNavigator> _navigator;
		public BTBlackboardValue<AL_AgentNavigator> Navigator { get => _navigator; set => _navigator = value; }
		BTBlackboardValue<Agent> _agent;
		public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

		public MoveToRandomPlaceTask(float maxDistance) : base()
		{
			this.maxDistance = maxDistance;
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Agent agent = Agent.GetValue();
			Vec3 randomPosition = CoreUtils.GetRandomPositionWithinRadius(agent.Position, maxDistance);
			WorldPosition destination = new WorldPosition(agent.Mission.Scene, randomPosition);
			Navigator.GetValue().SetTargetFrame(randomPosition.ToWorldPosition(), 0f);
			agent.RiderAgent?.GetComponent<AL_DefaultAgentComponent>()?.AgentNavigator.SetTargetFrame(randomPosition.ToWorldPosition(), 0f);
			return true;
		}
	}
}
