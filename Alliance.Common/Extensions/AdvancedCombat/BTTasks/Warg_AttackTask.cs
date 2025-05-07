using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class Warg_AttackTask : BTTask, IBTCombatBlackboard
	{
		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		BTBlackboardValue<Agent> target;
		public BTBlackboardValue<Agent> Target { get => target; set => target = value; }

		public Warg_AttackTask() : base()
		{
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Agent warg = Agent.GetValue();
			warg.WargAttack();

			return true;
		}
	}
}
