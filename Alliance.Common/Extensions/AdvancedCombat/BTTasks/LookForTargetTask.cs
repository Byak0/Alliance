using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class LookForTargetTask : BTTask, IBTCombatBlackboard
	{
		private readonly float range;

		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		BTBlackboardValue<Agent> target;
		public BTBlackboardValue<Agent> Target { get => target; set => target = value; }

		public LookForTargetTask(float range) : base()
		{
			this.range = range;
		}

		public override BTTaskStatus Execute()
		{
			Target.SetValue(GetBestTarget(range));

			if (Target.GetValue() != null) return BTTaskStatus.FinishedWithTrue;
			return BTTaskStatus.FinishedWithFalse;
		}

		private Agent GetBestTarget(float range)
		{
			Agent agent = Agent.GetValue();
			Team agentTeam = agent.Team ?? agent.RiderAgent?.Team;
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(range, agent);

			Agent bestTarget = null;
			float bestScore = float.MinValue;

			foreach (Agent potentialTarget in nearbyAgents)
			{
				if (potentialTarget == null || potentialTarget == agent || !potentialTarget.IsActive() ||
					(potentialTarget.Team != null && agentTeam != null && !potentialTarget.Team.IsEnemyOf(agentTeam)) ||
					(potentialTarget.RiderAgent?.Team != null && agentTeam != null && !potentialTarget.RiderAgent.Team.IsEnemyOf(agentTeam)))
					continue;

				float score = 0;

				FormationComponent formationComponent = potentialTarget.MissionPeer?.GetComponent<FormationComponent>();

				if (formationComponent != null && formationComponent.State == FormationState.Rambo)
					score += 40; // Prefer rambos

				if (potentialTarget.IsHorse() && potentialTarget.RiderAgent == null)
					score += 80; // Prefer riderless horses

				if (potentialTarget.Health / potentialTarget.HealthLimit < WargConstants.LOW_HEALTH_THRESHOLD)
					score += 20; // Prefer wounded targets

				float distance = (potentialTarget.Position - agent.Position).Length;
				score -= distance; // Prefer closer targets

				if (score > bestScore)
				{
					bestTarget = potentialTarget;
					bestScore = score;
				}
			}

			return bestTarget;
		}
	}
}
