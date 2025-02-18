using Alliance.Common.Extensions.AdvancedCombat.BTBehaviorTrees;
using BehaviorTrees;

namespace Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards
{
	public interface IBTStateBlackboard : IBTBlackboard
	{
		public BTBlackboardValue<BTState> State { get; set; }
	}
}
