using BehaviorTrees;
using BehaviorTreeWrapper.AbstractDecoratorsListeners;
using BehaviorTreeWrapper.BlackBoardClasses;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTDecorators
{
	public class HasRiderDecorator : BannerlordNoWaitDecorator, IBTBannerlordBase
	{
		BTBlackboardValue<Agent> _agent;
		public BTBlackboardValue<Agent> Agent { get => _agent; set => _agent = value; }

		private bool _hasRider;

		public HasRiderDecorator(bool hasRider = true) : base()
		{
			_hasRider = hasRider;
		}

		public override bool Evaluate()
		{
			if (_hasRider)
			{
				return Agent.GetValue()?.RiderAgent != null;
			}
			return Agent.GetValue()?.RiderAgent == null;
		}
	}
}
