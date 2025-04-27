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
			Navigator = new BTBlackboardValue<AL_AgentNavigator>(agent.GetComponent<AL_DefaultAgentComponent>().AgentNavigator);
			Agent = new BTBlackboardValue<Agent>(agent);
			Target = new BTBlackboardValue<Agent>(null);
			State = new BTBlackboardValue<BTState>(BTState.Idle);
		}

		public static new BehaviorTree? BuildTree(object[] objects)
		{
			if (objects[0] is not Agent agent) return null;

			BehaviorTree? tree = StartBuildingTree(new TrollBehaviorTree(agent))
				.AddSelector("main")
					.AddSelector("Idle", new StateDecorator(BTState.Idle))
						.AddSequence("UnderAttack", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
							// TODO add chance to ignore hit
							.AddTask(new LogTask("I am hit", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("Spotted", new AlarmedDecorator(SubscriptionPossibilities.OnSelfAlarmedStateChanged))
							// TODO add chance to ignore alarm
							.AddTask(new LogTask("I am spotted", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("IdleSeq", new WaitNSecondsTickDecorator(5))
							// TODO differentiate idle animations if in combat or not
							.AddTask(new LogTask("I am idle", LogLevel.Debug))
							//.AddTask(new AnimationTask(TrollConstants.IdleAnimations))
							//.AddTask(new AnimationBoneCheckTask(TrollConstants.RageAnimations[0], 2f, TrollConstants.CollisionBones, 1f))
							.Up()
						.Up()
					.AddSelector("LookForTarget", new StateDecorator(BTState.LookForTarget))
						.AddSequence("LookForTargetSeq")
							.AddTask(new LogTask("I am looking for target", LogLevel.Debug))
							.AddTask(new LookForTargetTask(TrollConstants.CHASE_RADIUS))
							.AddTask(new LogTask("I found a target", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.Chase))
							.Up()
						.AddSequence("BackToIdle")
							.AddTask(new LogTask("I found no target", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.Idle))
							.Up()
						.Up()
					.AddSelector("ChaseOrNot", new StateDecorator(BTState.Chase))
						.AddSequence("UnderAttack", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
							// TODO add chance to ignore hit
							.AddTask(new LogTask("I am hit", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("TryChase")
							.AddTask(new IsTargetCloseTask(TrollConstants.CHASE_RADIUS))
							.AddTask(new LogTask("I am chasing", LogLevel.Debug))
							.AddTask(new MoveToTargetTask())
							// TODO only play animation once in a while
							//.AddTask(new AnimationTask(TrollConstants.RageAnimations))
							//.AddTask(new AnimationBoneCheckTask(TrollConstants.RageAnimations[0], 2f, TrollConstants.CollisionBones, 1f))
							.Up()
						.AddSequence("CancelChase")
							.AddTask(new LogTask("I lost my target", LogLevel.Debug))
							// TODO only play animation once in a while
							//.AddTask(new AnimationTask(TrollConstants.SearchAnimations))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.Up()
					.Up()
				.Finish();
			return tree;
		}
	}
}
