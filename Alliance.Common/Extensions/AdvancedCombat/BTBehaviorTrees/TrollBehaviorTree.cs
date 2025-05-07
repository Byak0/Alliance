using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using Alliance.Common.Extensions.AdvancedCombat.BTDecorators;
using Alliance.Common.Extensions.AdvancedCombat.BTTasks;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using BehaviorTrees;
using BehaviorTreeWrapper;
using BehaviorTreeWrapper.Decorators;
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
							.AddTask(new LogTask(agent.Index + "-I am hit", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("Spotted", new AlarmedDecorator(SubscriptionPossibilities.OnSelfAlarmedStateChanged))
							.AddTask(new LogTask(agent.Index + "-I am spotted", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("IdleSeq", new WaitNSecondsTickDecorator(5))
							.AddTask(new LogTask(agent.Index + "-I am idle", LogLevel.Debug))
							.AddTask(new AnimationTask(TrollConstants.IdleAnimations))
							.Up()
						.Up()
					.AddSelector("LookForTarget", new StateDecorator(BTState.LookForTarget))
						.AddSequence("LookForTargetSeq")
							.AddTask(new LogTask(agent.Index + "-I am looking for target", LogLevel.Debug))
							.AddTask(new LookForTargetTask(TrollConstants.CHASE_RADIUS))
							.AddTask(new LogTask(agent.Index + "-I found a target", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.Chase))
							.Up()
						.AddSequence("BackToIdle")
							.AddTask(new LogTask(agent.Index + "-I found no target", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.Idle))
							.Up()
						.Up()
					.AddSelector("ChaseOrNot", new StateDecorator(BTState.Chase))
						.AddSequence("UnderAttack", new HitDecorator(SubscriptionPossibilities.OnSelfIsHit))
							.AddTask(new LogTask(agent.Index + "-I am hit", LogLevel.Debug))
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.AddSequence("EnemiesTooClose", new EnemiesTooCloseDecorator())
							.AddTask(new PlayAnimationAndCheckCollisionTask(ActionIndexCache.Create("act_kick_right_leg"), TrollConstants.FootCollisionBones, 15f, 0.6f, 0.4f, 0.95f, false,
										(Agent agt, Agent tgt, sbyte bone) =>
										{
											if (!tgt.IsTroll())
											{
												tgt.ProjectAgent(agt.Position, 1f);
												agt.DealDamage(tgt, 50);
											}
										}))
							.Up()
						.AddSequence("TryChase")
							.AddTask(new IsTargetCloseTask(TrollConstants.CHASE_RADIUS))
							.AddTask(new LogTask(agent.Index + "-I am chasing", LogLevel.Debug))
							.AddTask(new MoveToTargetTask())
							.AddRandomSelector("Random", new WaitNSecondsTickDecorator(5))
								.AddSequence("Nothing", 70)
									.Up()
								.AddSequence("ChaseAnim", 30)
									.AddTask(new AnimationTask(TrollConstants.RageAnimations))
									.Up()
								.Up()
							.Up()
						.AddSequence("CancelChase")
							.AddTask(new LogTask(agent.Index + "-I lost my target", LogLevel.Debug))
							.AddTask(new ClearTargetTask())
							.AddRandomSelector("Random", new WaitNSecondsTickDecorator(3))
								.AddSequence("Nothing", 50)
									.Up()
								.AddSequence("CancelChaseAnim", 50)
									.AddTask(new AnimationTask(TrollConstants.SearchAnimations))
									.Up()
							.AddTask(new SetStateTask(BTState.LookForTarget))
							.Up()
						.Up()
				.Finish();
			return tree;
		}
	}
}
