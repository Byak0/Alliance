using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class Warg_FindClosestEnemyTask : BTTask, IBTCombatBlackboard
	{
		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		BTBlackboardValue<Agent> target;
		public BTBlackboardValue<Agent> Target { get => target; set => target = value; }

		public Warg_FindClosestEnemyTask() : base()
		{
		}

		public override BTTaskStatus Execute()
		{
			Agent warg = Agent.GetValue();
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(8f, warg);
			Agent _target = nearbyAgents.FirstOrDefault(agt =>
				IsValidTarget(warg, agt)
				&& CloseEnoughForAttack(warg, agt)
			);
			Target.SetValue(_target);

			if (Target.GetValue() != null) return BTTaskStatus.FinishedWithTrue;

			return BTTaskStatus.FinishedWithFalse;
		}

		private bool IsValidTarget(Agent warg, Agent agt)
		{
			// 1) Never target yourself or any other warg or your own rider
			if (agt == warg || agt.IsWarg() || agt == warg.RiderAgent) return false;

			// 2) If this warg has a rider
			if (warg.RiderAgent != null)
			{
				var wargTeam = warg.RiderAgent.Team;
				bool agentIsEnemy = agt.Team != null && wargTeam != null && agt.Team.IsEnemyOf(wargTeam);
				bool riderIsEnemy = agt.RiderAgent != null
									 && agt.RiderAgent.Team != null
									 && wargTeam != null
									 && agt.RiderAgent.Team.IsEnemyOf(wargTeam);
				bool isNeutral = agt.Team == null;

				return agentIsEnemy
					|| riderIsEnemy
					|| isNeutral;
			}
			// 3) If this warg is riderless
			else
			{
				// Target anyone except warg riders
				return agt.MountAgent == null
					|| !agt.MountAgent.IsWarg();
			}
		}

		private bool CloseEnoughForAttack(Agent warg, Agent target)
		{
			float distanceToTarget = (target.Position - warg.Position).Length;
			float distanceForAttack = 2f + warg.MovementVelocity.Y;
			bool closeEnoughForAttack = distanceToTarget <= distanceForAttack;
			bool frontAttack = target.IsInFrontCone(warg, 30);

			// Close enough to attack and target is in front
			if (closeEnoughForAttack && frontAttack)
			{
				return true;
			}
			return false;
		}
	}
}
