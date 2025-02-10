using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using Alliance.Common.Extensions.AdvancedCombat.BTTasks;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.Decorators;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.BTBehaviorTrees
{
	public enum BTState
	{
		Idle,
		LookForTarget,
		Chase,
		MeleeFight,
		RangedFight,
		Flee
	}

	public class StateDecorator : AbstractDecorator, IBTStateBlackboard
	{
		private readonly BTState targetState;

		public StateDecorator(BTState targetState) : base()
		{
			this.targetState = targetState;
		}

		BTBlackboardValue<BTState> state;
		public BTBlackboardValue<BTState> State { get => state; set => state = value; }

		public override bool Evaluate()
		{
			return targetState == State.GetValue();
		}
	}

	public class SetStateTask : BTTask, IBTStateBlackboard
	{
		private readonly BTState targetState;

		BTBlackboardValue<BTState> state;
		public BTBlackboardValue<BTState> State { get => state; set => state = value; }

		public SetStateTask(BTState targetState) : base()
		{
			this.targetState = targetState;
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			await Task.CompletedTask;
			State.SetValue(targetState);
			return true;
		}
	}

	public class IsTargetCloseTask : BTTask, IBTCombatBlackboard
	{
		private readonly float maxRange;

		public BTBlackboardValue<Agent> Target { get; set; }
		public BTBlackboardValue<Agent> Agent { get; set; }

		public IsTargetCloseTask(float maxRange) : base()
		{
			this.maxRange = maxRange;
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Agent self = Agent.GetValue();
			Agent targetAgent = Target.GetValue();

			if (self == null || targetAgent == null || targetAgent.Health <= 0 || targetAgent.IsFadingOut()
				|| self.Position.Distance(targetAgent.Position) > maxRange)
			{
				Target.SetValue(null);
				return false;
			}
			return true;
		}
	}

	public class TrollBehaviorTree : BehaviorTree, IBTCombatBlackboard, IBTStateBlackboard, IBTMobile
	{
		public BTBlackboardValue<AL_AgentNavigator> Navigator { get; set; }
		public BTBlackboardValue<Agent> Agent { get; set; }
		public BTBlackboardValue<Agent> Target { get; set; }
		public BTBlackboardValue<BTState> State { get; set; }

		public TrollBehaviorTree(Agent agent) : base()
		{
			Navigator = new BTBlackboardValue<AL_AgentNavigator>(agent.GetComponent<DefaultAgentComponent>().CreateAgentNavigator());
			Agent = new BTBlackboardValue<Agent>(agent);
			Target = new BTBlackboardValue<Agent>(null);
			State = new BTBlackboardValue<BTState>(BTState.Idle);
		}

		public static new BehaviorTree? BuildTree(object[] objects)
		{
			if (objects[0] is not Agent agent) return null;

			BehaviorTree? tree = StartBuildingTree(new TrollBehaviorTree(agent))
				.AddSelector("main")
					.AddSequence("Idle", new StateDecorator(BTState.Idle))
						.AddSequence("UnderAttack", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
							.AddTask(new LogTask("I am hit", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("Spotted", new AlarmedDecorator(SubscriptionPossibilities.OnSelfAlarmedStateChanged))
							.AddTask(new LogTask("I am spotted", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("IdleSeq", new StateDecorator(BTState.Idle))
							.AddTask(new LogTask("I am idle", LogLevel.Debug))
							.AddTask(new AnimationTask(TrollConstants.IdleAnimations))
							.Up()
						.Up()
					.AddSequence("LookForTarget", new StateDecorator(BTState.LookForTarget))
						.AddSequence("LookForTargetSeq")
							.AddTask(new LogTask("I am looking for target", LogLevel.Debug))
							.AddTask(new LookForTargetTask(TrollConstants.CHASE_RADIUS))
							.AddTask(new LogTask("I found a target", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.Chase))
							.Up()
						.Up()
					.AddSequence("Chase", new StateDecorator(BTState.Chase))
						.AddSelector("ChaseOrNot")
							.AddSequence("TryChase")
								.AddTask(new IsTargetCloseTask(TrollConstants.CHASE_RADIUS))
								.AddTask(new AnimationTask(TrollConstants.RageAnimations))
								.AddTask(new LogTask("I am chasing", LogLevel.Debug))
								.AddTask(new MoveToTargetTask())
								.Up()
							.AddSequence("CancelChase")
								.AddTask(new LogTask("I lost my target", LogLevel.Debug))
								.AddTask(new AnimationTask(TrollConstants.SearchAnimations))
								.AddTask(new SetStateTask(BTState.LookForTarget))
								.Up()
							.Up()
						.Up()
					.Up()
				.Finish();
			return tree;
		}
	}
}
