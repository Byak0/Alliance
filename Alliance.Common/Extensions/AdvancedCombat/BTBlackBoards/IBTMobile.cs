using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using BehaviorTrees;

namespace Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards
{
	public interface IBTMobile : IBTBlackboard
	{
		public BTBlackboardValue<AL_AgentNavigator> Navigator { get; set; }
	}
}
