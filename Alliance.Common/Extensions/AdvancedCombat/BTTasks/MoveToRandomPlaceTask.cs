using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper.BlackBoardClasses;
using System.Threading;
using System.Threading.Tasks;
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
			Navigator.GetValue().SetTargetFrame(randomPosition.ToWorldPosition(), 0f, 1f, -10, TaleWorlds.MountAndBlade.Agent.AIScriptedFrameFlags.GoToPosition);
			return true;
		}
	}
}
