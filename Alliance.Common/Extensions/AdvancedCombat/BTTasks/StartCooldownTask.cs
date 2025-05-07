using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class StartCooldownTask : BTTask, IBTTimerBlackboard
	{
		public BTBlackboardValue<float> Timer { get; set; }

		public StartCooldownTask() : base()
		{
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Timer.SetValue(Mission.Current.GetMissionTimeInSeconds());
			return true;
		}
	}
}
