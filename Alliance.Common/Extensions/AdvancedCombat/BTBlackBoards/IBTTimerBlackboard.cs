using BehaviorTrees;

namespace Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards
{
	public interface IBTTimerBlackboard : IBTBlackboard
	{
		public BTBlackboardValue<float> Timer { get; set; }
	}
}