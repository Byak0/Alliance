using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentComponents
{
	/// <summary>
	/// TODO - Allow the troll to behave independently.
	/// </summary>
	public class TrollComponent : AdvancedCombatComponent
	{
		private float _refreshTimer = 0f;
		private float _kickCD = 0f;

		public float KickCD => _kickCD;

		public TrollComponent(Agent agent) : base(agent)
		{
		}

		public override void OnTickAsAI(float dt)
		{
			_refreshTimer += dt;
			_kickCD += dt;
			if (_refreshTimer > 1)
			{
				_refreshTimer = 0;
				if (_kickCD > 8)
				{
					TryKick();
				}
			}
		}

		private void TryKick()
		{
			if (Agent.GetCurrentAnimationFlag(1).HasFlag(AnimFlags.amf_priority_attack)) return;

			MBList<Agent> nearbyAgents = new MBList<Agent>();
			Mission.Current.GetNearbyEnemyAgents(Agent.Position.AsVec2, 2.5f, Agent.Team, nearbyAgents);
			nearbyAgents.RemoveAll(agent => agent.IsTroll() || agent.IsEnt());

			if (nearbyAgents.Count == 0)
			{
				return;
			}

			// todo move this to a constant and change animation to custom one
			Animation kickAnimation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_kick_right_leg"); // +

			AnimationSystem.Instance.PlayAnimation(Agent, kickAnimation, true);

			foreach (Agent victim in nearbyAgents)
			{
				if (AdvancedCombatHelper.IsInFrontCone(Agent, victim, 120))
				{
					float projectionForce = 2f - victim.Position.Distance(Agent.Position) / 2f;
					AdvancedCombatHelper.ProjectAgent(victim, Agent.Position, projectionForce);
				}
			}

			_kickCD = 0;
		}

		public override void OnTick(float dt)
		{
			return;
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(30, Agent.Position);
			foreach (Agent nearbyAgent in nearbyAgents)
			{
				if (nearbyAgent.GetTargetAgent() == Agent && nearbyAgent.Position.Distance(Agent.Position) < 20)
				{
					Vec2 newTargetPosition = (Agent.Position + nearbyAgent.LookDirection * -5f).AsVec2;
					//nearbyAgent.ClearTargetFrame();
					//if (nearbyAgent.AIStateFlags != Agent.AIStateFlag.Paused)
					//{
					//	nearbyAgent.SetTargetPosition(newTargetPosition);
					//	nearbyAgent.AIStateFlags = Agent.AIStateFlag.Paused;
					//}
				}
				else
				{
					//nearbyAgent.ClearTargetFrame();
					//nearbyAgent.AIStateFlags = Agent.AIStateFlag.Alarmed;
				}
			}
		}
	}
}
