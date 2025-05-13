using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class CooldownAvailableDecorator : BannerlordNoWaitDecorator, IBTTimerBlackboard
	{
		public BTBlackboardValue<float> Timer { get; set; }

		private float _cooldown;

		public CooldownAvailableDecorator(float cooldown) : base()
		{
			_cooldown = cooldown;
		}

		public override bool Evaluate()
		{
			return Timer.GetValue() + _cooldown <= Mission.Current.GetMissionTimeInSeconds();
		}
	}
}
