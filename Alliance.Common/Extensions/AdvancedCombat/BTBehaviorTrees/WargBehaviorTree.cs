using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using Alliance.Common.Extensions.AdvancedCombat.BTDecorators;
using Alliance.Common.Extensions.AdvancedCombat.BTTasks;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using BehaviorTrees;
using BehaviorTreeWrapper.BlackBoardClasses;
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

		public WargBehaviorTree(Agent agent) : base(1000)
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
				.AddSequence("main")
					.AddTask(new RandomWaitTask(0f, 1f))

					.AddSelector("mainSel")
						// When warg is mounted, only check for attack
						.AddSequence("HasRider", new HasRiderDecorator(true))
							.AddTask(new LogTask(agent.Index + "-HasRider", LogLevel.Debug))
							.AddSubTree("WargAttackTree", 100, objects)
							.Up()

						// Otherwise, move to target & attack
						.AddSequence("NoRider", new HasRiderDecorator(false))
							.AddTask(new LogTask(agent.Index + "-NoRider", LogLevel.Debug))
							.AddTask(new Warg_LookForTargetTask(WargConstants.CHASE_RADIUS))
							.AddTask(new MoveToTargetTask())
							.AddSubTree("WargAttackTree", 100, objects)
							.Up()

						// Backup action if previous sequences were not successful
						.AddSequence("Idle")
							.AddTask(new LogTask(agent.Index + "-Idle", LogLevel.Debug))
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
				.AddSelector("main")
					.AddSequence("TryToAttack", new CooldownAvailableDecorator(WargConstants.ATTACK_COOLDOWN))
						.AddTask(new LogTask(agent.Index + "-Trying to attack", LogLevel.Debug))
						.AddTask(new Warg_FindClosestEnemyTask())
						.AddTask(new Warg_AttackTask())
						.AddTask(new LogTask(agent.Index + "-Attacking", LogLevel.Debug))
						.AddTask(new StartCooldownTask())
						.Up()
					.AddSequence("Cancel")
						.AddTask(new LogTask(agent.Index + "-Cancelled attack", LogLevel.Debug))
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
				.AddSelector("Random", new AlwaysTrueDecorator())
					.AddSequence("Nothing", 50)
						.AddTask(new LogTask(agent.Index + "-Doing nothing", LogLevel.Debug))
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
