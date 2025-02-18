using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Target.SetValue(GetBestTarget(range));

			return Target.GetValue() != null;
		}

		private Agent GetBestTarget(float range)
		{
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(range, agent.GetValue());

			Agent bestTarget = null;
			float bestScore = float.MinValue;

			foreach (Agent potentialTarget in nearbyAgents)
			{
				if ((potentialTarget == agent.GetValue() || !potentialTarget.IsActive()) || (potentialTarget.Team != null && !potentialTarget.Team.IsEnemyOf(agent.GetValue().Team)))
					continue;

				float score = 0;

				FormationComponent formationComponent = potentialTarget.MissionPeer?.GetComponent<FormationComponent>();

				if (formationComponent != null && formationComponent.State == FormationState.Rambo)
					score += 40; // Prefer rambos

				//if (potentialTarget.Team != Agent.Team)
				//	score += 40; // Prefer enemies

				if (potentialTarget.IsMount && potentialTarget.RiderAgent == null) // TODO : replace IsMount by IsHorse when available
					score += 80; // Prefer riderless horses

				if (potentialTarget.Health / potentialTarget.HealthLimit < WargConstants.LOW_HEALTH_THRESHOLD)
					score += 20; // Prefer wounded targets

				float distance = (potentialTarget.Position - agent.GetValue().Position).Length;
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
