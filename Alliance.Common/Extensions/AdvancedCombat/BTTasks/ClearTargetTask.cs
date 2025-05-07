using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class ClearTargetTask : BTTask, IBTMobile, IBTCombatBlackboard
	{
		BTBlackboardValue<AL_AgentNavigator> _navigator;
		public BTBlackboardValue<AL_AgentNavigator> Navigator { get => _navigator; set => _navigator = value; }
		BTBlackboardValue<Agent> _agent;
		public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }
		BTBlackboardValue<Agent> _target;
		public BTBlackboardValue<Agent> Target { get => _target; set => _target = value; }

		public ClearTargetTask() : base() { }

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Target.SetValue(null);
			Navigator.GetValue().SetTarget(null, AL_AgentNavigator.FollowStrategy.Default);
			return true;
		}
	}
}
