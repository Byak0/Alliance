using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentComponents
{
	/// <summary>
	/// Advanced combat mechanics for standard humanoid races (human/elves/uruks etc.).
	/// </summary>
	public class HumanoidComponent : AL_DefaultAgentComponent
	{
		private Agent _threat;
		private Agent _target;
		private float _forgetThreatTimer;

		public Agent Threat => _threat;
		public Agent Target => _target;
		public float ForgetThreatTimer => _forgetThreatTimer;

		public HumanoidComponent(Agent agent) : base(agent)
		{
		}

		public override void OnTickAsAI(float dt)
		{
			if (_threat == null)
			{
				return;
			}

			if (!Agent.IsActive() || Agent.IsRunningAway || _threat.Health <= 0 || _threat.IsFadingOut() || _threat.RiderAgent?.Team == Agent.Team)
			{
				ClearThreat();
				ClearTarget();
				return;
			}

			if (_threat.Position.Distance(Agent.Position) > 30f)
			{
				_forgetThreatTimer += dt;
				if (_forgetThreatTimer >= 10)
				{
					ClearThreat();
				}
				if (_target != null)
				{
					ClearTarget();
				}
				return;
			}

			SetTarget(_threat);
		}

		public void SetThreat(Agent threat)
		{
			_forgetThreatTimer = 0;
			_threat = threat;
		}

		public void ClearThreat()
		{
			_threat = null;
		}

		public void SetTarget(Agent target)
		{
			_target = target;
			if (target.IsMount)
			{
				WorldPosition pos = Agent.GetWorldPosition();
				if (Agent.HasRangedWeapon(true))
				{
					Agent.SetScriptedTargetEntityAndPosition(target.AgentVisuals.GetEntity(), pos, Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation, false);
					Agent.SetScriptedPosition(ref pos, false, Agent.AIScriptedFrameFlags.RangerCanMoveForClearTarget);
				}
				else
				{
					Agent.SetScriptedTargetEntityAndPosition(target.AgentVisuals.GetEntity(), pos, Agent.AISpecialCombatModeFlags.AttackEntity, false);
					Agent.SetMaximumSpeedLimit(Agent.Monster.WalkingSpeedLimit * 3f, false);
				}
			}
			else
			{
				Agent.SetTargetAgent(target);
			}
		}

		public void ClearTarget()
		{
			_target = null;
			Agent.DisableScriptedCombatMovement();
			Agent.DisableScriptedMovement();
		}

		public override void OnMissionResultReady(MissionResult missionResult)
		{
			ClearThreat();
			ClearTarget();
		}
	}
}
