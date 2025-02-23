using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards
{
	public interface IBTCombatBlackboard : IBTBannerlordBase
	{
		public BTBlackboardValue<Agent> Target { get; set; }
	}
}
