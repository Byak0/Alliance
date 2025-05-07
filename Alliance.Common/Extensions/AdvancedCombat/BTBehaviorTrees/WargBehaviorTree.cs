using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using Alliance.Common.Extensions.AdvancedCombat.BTDecorators;
using Alliance.Common.Extensions.AdvancedCombat.BTTasks;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.BlackBoardClasses;
using BehaviorTreeWrapper.Decorators;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.BTBehaviorTrees
{
	public class WargBehaviorTree : BehaviorTree, IBTCombatBlackboard, IBTStateBlackboard, IBTMobile, IBTTimerBlackboard
	{
		public BTBlackboardValue<AL_AgentNavigator> Navigator { get; set; }
		public BTBlackboardValue<Agent> Agent { get; set; }
		public BTBlackboardValue<Agent> Target { get; set; }
		public BTBlackboardValue<BTState> State { get; set; }
		public BTBlackboardValue<float> Timer { get; set; }

		public WargBehaviorTree(Agent agent) : base(250)
		{
			Navigator = new BTBlackboardValue<AL_AgentNavigator>(agent.GetComponent<AL_DefaultAgentComponent>().AgentNavigator);
			Agent = new BTBlackboardValue<Agent>(agent);
			Target = new BTBlackboardValue<Agent>(null);
			State = new BTBlackboardValue<BTState>(BTState.Idle);
			Timer = new BTBlackboardValue<float>(0f);
		}

		public static new BehaviorTree? BuildTree(object[] objects)
		{
			if (objects[0] is not Agent agent) return null;

			BehaviorTree? tree = StartBuildingTree(new WargBehaviorTree(agent))
				.AddSelector("main")

					// When warg is mounted, only check for attack
					.AddSelector("HasRider", new HasRiderDecorator())
						.AddSubTree("WargAttackTree", 100, objects)
						.Up()

					// Otherwise, act depending on state
					.AddSelector("NoRider", new HasRiderDecorator(false))
						// Idle state - wait for a combat event
						.AddSelector("Idle", new StateDecorator(BTState.Idle))
							.AddSequence("UnderAttack", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
								.AddTask(new LogTask(agent.Index + "-I am hit", LogLevel.Debug))
								.AddTask(new SetStateTask(BTState.LookForTarget))
								.Up()
							.AddSequence("Spotted", new AlarmedDecorator(SubscriptionPossibilities.OnSelfAlarmedStateChanged))
								.AddTask(new LogTask(agent.Index + "-I am spotted", LogLevel.Debug))
								.AddTask(new SetStateTask(BTState.LookForTarget))
								.Up()
							.AddSequence("IdleSeq", new WaitNSecondsTickDecorator(5))
								.AddSubTree("WargIdleTree", 100, objects)
								// Having two sequences in cascade seems to ignore second sequence decorator result ? Putting a selector inbetween
								.AddSelector("IdleSeqqx")
									.AddSequence("IdleSeq-LookForTarget", new RandomChanceDecorator(0.1f))
										.AddTask(new SetStateTask(BTState.LookForTarget))
										.Up()
									.Up()
								.Up()
							.Up()

						// Look for target state - try to find a target
						.AddSelector("LookForTarget", new StateDecorator(BTState.LookForTarget))
							.AddSequence("LookForTargetSeq")
								.AddTask(new LogTask(agent.Index + "-I am looking for target", LogLevel.Debug))
								.AddTask(new Warg_LookForTargetTask(WargConstants.CHASE_RADIUS))
								.AddTask(new LogTask(agent.Index + "-I found a target", LogLevel.Debug))
								.AddTask(new SetStateTask(BTState.Chase))
								.AddTask(new MoveToTargetTask())
								.Up()
							.AddSequence("BackToIdle")
								.AddTask(new LogTask(agent.Index + "-I found no target", LogLevel.Debug))
								.AddTask(new SetStateTask(BTState.Idle))
								.Up()
							.Up()

						// Chase state - try to chase the target
						.AddSelector("ChaseOrNot", new StateDecorator(BTState.Chase))
							.AddSubTree("WargAttackTree", 100, objects)
							.AddSequence("LoseInterest", new RandomChanceDecorator(0.05f))
								.AddTask(new LogTask(agent.Index + "-I lost interest", LogLevel.Debug))
								.AddTask(new ClearTargetTask())
								.AddTask(new SetStateTask(BTState.Idle))
								.Up()
							.AddSequence("TryChase")
								.AddTask(new IsTargetCloseTask(WargConstants.CHASE_RADIUS))
								.Up()
							.AddSequence("CancelChase")
								.AddTask(new LogTask("I lost my target", LogLevel.Debug))
								.AddTask(new ClearTargetTask())
								.AddTask(new SetStateTask(BTState.LookForTarget))
								.Up()
							.Up()
						.Up()
					.Up()

				.Finish();
			return tree;
		}
	}

	public class WargAttackBehaviorTree : BehaviorTree, IBTCombatBlackboard, IBTStateBlackboard, IBTMobile, IBTTimerBlackboard
	{
		public BTBlackboardValue<AL_AgentNavigator> Navigator { get; set; }
		public BTBlackboardValue<Agent> Agent { get; set; }
		public BTBlackboardValue<Agent> Target { get; set; }
		public BTBlackboardValue<BTState> State { get; set; }
		public BTBlackboardValue<float> Timer { get; set; }

		public WargAttackBehaviorTree(Agent agent) : base()
		{
			Navigator = new BTBlackboardValue<AL_AgentNavigator>(agent.GetComponent<AL_DefaultAgentComponent>().AgentNavigator);
			Agent = new BTBlackboardValue<Agent>(agent);
			Target = new BTBlackboardValue<Agent>(null);
			State = new BTBlackboardValue<BTState>(BTState.Idle);
			Timer = new BTBlackboardValue<float>(0f);
		}

		public static new BehaviorTree? BuildTree(object[] objects)
		{
			if (objects[0] is not Agent agent) return null;

			BehaviorTree? tree = StartBuildingTree(new WargAttackBehaviorTree(agent))
				.AddSelector("WargAttack")
					.AddSequence("TryToAttack", new CooldownAvailableDecorator(WargConstants.ATTACK_COOLDOWN))
						.AddTask(new Warg_FindClosestEnemyTask())
						.AddTask(new Warg_AttackTask())
						.AddTask(new StartCooldownTask())
						.Up()
					.Up()
				.Finish();
			return tree;
		}
	}

	public class WargIdleBehaviorTree : BehaviorTree, IBTMobile, IBTBannerlordBase
	{
		public BTBlackboardValue<AL_AgentNavigator> Navigator { get; set; }
		public BTBlackboardValue<Agent> Agent { get; set; }

		public WargIdleBehaviorTree(Agent agent) : base()
		{
			Navigator = new BTBlackboardValue<AL_AgentNavigator>(agent.GetComponent<AL_DefaultAgentComponent>().AgentNavigator);
			Agent = new BTBlackboardValue<Agent>(agent);
		}

		public static new BehaviorTree? BuildTree(object[] objects)
		{
			if (objects[0] is not Agent agent) return null;

			BehaviorTree? tree = StartBuildingTree(new WargIdleBehaviorTree(agent))
				.AddRandomSelector("Random", new AlwaysTrueDecorator())
					.AddSequence("Nothing", 50)
						.Up()
					.AddSequence("Move", 25)
						.AddTask(new MoveToRandomPlaceTask(30f))
						.Up()
					.AddSequence("IdleAnim", 25)
						.AddTask(new AnimationTask(WargConstants.IdleAnimations))
						.Up()
					.Up()
				.Finish();
			return tree;
		}
	}
}
