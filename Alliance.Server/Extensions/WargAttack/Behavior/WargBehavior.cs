using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Server.Extensions.WargAttack.Models;
using Alliance.Server.Extensions.WargAttack.Utilities;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.WargAttack.Behavior
{

	/// <summary>
	/// Ties a WargComponent to wargs when they spawn.
	/// </summary>
	public class WargBehavior : MissionNetwork, IMissionBehavior
	{
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			if (agent.Monster.StringId == "warg")
			{
				agent.AddComponent(new WargComponent(agent));
				Log("Added WargComponent to agent", LogLevel.Debug);
			}
		}
	}

	/// <summary>
	/// Allow the warg to behave independently. Hunting, chilling, fleeing, etc.
	/// </summary>
	public class WargComponent : AgentComponent
	{
		private Agent _target;
		private float _refreshDelay;
		private float _targetChangeDelay;
		private MonsterState _currentState;
		private bool _isWounded;
		private ChaseTrajectory _chaseTrajectory;

		public enum ChaseTrajectory
		{
			Straight,
			CurveLeft,
			CurveRight
		}

		public enum MonsterState
		{
			None,
			Idle,
			Chase,
			Attack,
			Flee,
			Careful
		}

		public WargComponent(Agent agent) : base(agent)
		{
			// Make sure warg has a team assigned. If not, assign him a random team.
			if (Agent.Team == null)
			{
				if (Agent.RiderAgent?.Team != null)
				{
					Agent.SetTeam(Agent.RiderAgent.Team, true);
				}
				else
				{
					Team randomTeam = MBRandom.RandomInt(0, 2) == 0 ? Mission.Current.AttackerTeam : Mission.Current.DefenderTeam;
					Agent.SetTeam(randomTeam, true);
				}
				Log($"In WargComponent, agent.Team was null ({Agent.Team})", LogLevel.Debug);
				return;
			}
		}

		public override void OnTickAsAI(float dt)
		{
			try
			{
				// Tick once every RefreshTime or CloseRangeRefreshTime if near target
				_refreshDelay += dt;
				_targetChangeDelay += dt;
				if ((_currentState == MonsterState.Chase || _currentState == MonsterState.Careful) && _target != null && (_target.Position - Agent.Position).Length < WargConstants.THREAT_RANGE)
				{
					if (_refreshDelay <= WargConstants.CLOSE_RANGE_AI_REFRESH_TIME) return;
				}
				else
				{
					if (_refreshDelay <= WargConstants.AI_REFRESH_TIME) return;
				}
				_refreshDelay = 0;

				if (Agent.RiderAgent != null)
				{
					if (Agent.RiderAgent.IsAIControlled)
					{
						DetermineTargetAndStateWithAIRider();
					}
				}
				else
				{
					// Update health status
					_isWounded = Agent.Health / Agent.HealthLimit < WargConstants.LOW_HEALTH_THRESHOLD;
					DetermineTargetAndState();
				}

				switch (_currentState)
				{
					case MonsterState.None: break;
					case MonsterState.Idle:
						RandomIdleBehavior();
						Log("Warg is idling", LogLevel.Debug);
						break;
					case MonsterState.Chase:
						ChaseTarget();
						Log("Warg is chasing " + _target?.Name, LogLevel.Debug);
						break;
					case MonsterState.Attack:
						AttackTarget();
						Log("Warg is attacking", LogLevel.Debug);
						break;
					case MonsterState.Flee:
						FleeBehavior();
						Log("Warg is fleeing", LogLevel.Debug);
						break;
					case MonsterState.Careful:
						MaintainDistanceFromTarget();
						Log("Warg is keeping its distance from target with torch", LogLevel.Debug);
						break;
				}
			}
			catch (Exception e)
			{
				Log("Exception in WargBehavior : " + e.Message, LogLevel.Error);
			}
		}

		private void DetermineTargetAndStateWithAIRider()
		{
			// Check if there is enemy close by
			MBList<Agent> nearbyAgents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(Agent.Position.AsVec2, 5f, nearbyAgents);
			_target = nearbyAgents.FirstOrDefault(agt => agt != Agent.RiderAgent);

			if (_target == null)
			{
				_currentState = MonsterState.None;
				return;
			}

			if (ShouldAttack(50))
			{
				_currentState = MonsterState.Attack;
				return;
			}

			_currentState = MonsterState.None;
		}

		private void DetermineTargetAndState()
		{
			// 5 % chance to flee when wounded
			if (ShouldFlee(0.05f))
			{
				_currentState = MonsterState.Flee;
				return;
			}

			// 95% chance to be careful when nearby unit is dangerous/wield torch
			if (ShouldBeCareful(0.95f, out Agent _threat))
			{
				if (_target != _threat && _threat != null)
				{
					_target = _threat;
					_targetChangeDelay = 0;
				}
				_currentState = MonsterState.Careful;
				return;
			}

			// 50% chance of attacking target if possible
			if (ShouldAttack(0.5f))
			{
				_currentState = MonsterState.Attack;
				return;
			}

			// 25% chance of targetting someone if no current target
			if (ShouldChaseSomeone(0.25f, out Agent _bestTarget))
			{
				if (_target != _bestTarget && _bestTarget != null)
				{
					_target = _bestTarget;
				}
				_currentState = MonsterState.Chase;
				return;
			}

			// Idle if nothing else
			_currentState = MonsterState.Idle;
		}

		private bool ShouldAttack(float probability)
		{
			if (_target == null || _target.Health <= 0) return false;

			float distanceToTarget = (_target.Position - Agent.Position).Length;
			float distanceForAttack = Agent.MovementVelocity.Y >= 4 ? 5f : 3f;
			bool closeEnoughForAttack = distanceToTarget <= distanceForAttack;
			bool frontAttack = WargAttackHelper.IsInFrontCone(Agent, _target, 45);
			Log($"close enough ? {closeEnoughForAttack} ({distanceForAttack}/{distanceToTarget}) front ? {frontAttack}", LogLevel.Debug);

			// Close enough to attack and target is in front
			if (MBRandom.RandomFloat < probability && closeEnoughForAttack && frontAttack)
			{
				return true;
			}
			return false;
		}

		private bool ShouldChaseSomeone(float probability, out Agent _bestTarget)
		{
			_bestTarget = null;
			if (_target == null || _target != null && TargetIsNoLongerRelevant())
			{
				if (MBRandom.RandomFloat < probability)
				{
					_bestTarget = GetBestTarget();
					_targetChangeDelay = 0;
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				// Continue chasing current target
				_bestTarget = _target;
				return true;
			}
		}

		private bool TargetIsNoLongerRelevant()
		{
			return _target != null
				&& (!_target.IsActive() // Target is no longer active
					|| _target.Health <= 0 // or is dead
					|| (_target.Position - Agent.Position).Length > WargConstants.CHASE_RADIUS // or is too far away
					|| _targetChangeDelay >= WargConstants.TARGET_LOSE_INTEREST_TIME); // or have been chased for too long
		}

		private bool ShouldBeCareful(float probability, out Agent _threat)
		{
			// Check if anyone is a threat, triggering careful state
			_threat = null;
			Agent nearbyThreat = NearbyThreat();
			if (nearbyThreat != null && MBRandom.RandomFloat < probability)
			{
				_threat = nearbyThreat;
				return true;
			}
			return false;
		}

		private bool ShouldFlee(float probability)
		{
			return _currentState == MonsterState.Flee || _isWounded && MBRandom.RandomFloat < probability;
		}

		private Agent GetBestTarget()
		{
			MBList<Agent> nearbyAgents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(Agent.Position.AsVec2, WargConstants.CHASE_RADIUS, nearbyAgents);

			Agent bestTarget = null;
			float bestScore = float.MinValue;

			foreach (Agent potentialTarget in nearbyAgents)
			{
				if (potentialTarget == Agent || !potentialTarget.IsActive())
					continue;

				float score = 0;

				FormationComponent formationComponent = potentialTarget.MissionPeer?.GetComponent<FormationComponent>();
				if (formationComponent != null && formationComponent.State == FormationState.Rambo)
					score += 40; // Prefer rambos

				if (potentialTarget.Team != Agent.Team)
					score += 40; // Prefer enemies

				if (potentialTarget.Monster.StringId != "warg" && potentialTarget.MountAgent?.Monster.StringId != "warg")
					score += 30; // Prefer non-wargs

				if (potentialTarget.Health / potentialTarget.HealthLimit < WargConstants.LOW_HEALTH_THRESHOLD)
					score += 20; // Prefer wounded targets

				if (IsWieldingTorch(potentialTarget))
					score -= 40; // Avoid targets with torch

				float distance = (potentialTarget.Position - Agent.Position).Length;
				score -= distance; // Prefer closer targets

				if (score > bestScore)
				{
					bestTarget = potentialTarget;
					bestScore = score;
				}
			}

			return bestTarget;
		}

		private static bool IsWieldingTorch(Agent potentialTarget)
		{
			return potentialTarget.WieldedWeapon.Item != null
					&& potentialTarget.WieldedWeapon.Item.StringId.ToLower().Contains("torch")
				|| potentialTarget.WieldedOffhandWeapon.Item != null
					&& potentialTarget.WieldedOffhandWeapon.Item.StringId.ToLower().Contains("torch");
		}

		private Agent NearbyThreat()
		{
			MBList<Agent> nearbyAgents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(Agent.Position.AsVec2, WargConstants.THREAT_RANGE, nearbyAgents);

			foreach (Agent agent in nearbyAgents)
			{
				if (agent != Agent && agent.IsActive() && IsWieldingTorch(agent))
				{
					return agent;
				}
			}

			return null;
		}

		private void RandomIdleBehavior()
		{
			if (MBRandom.RandomFloat < 0.3f) // 30% chance to play a random animation while idling
			{
				AnimationSystem.Instance.PlayAnimation(Agent, WargConstants.IdleAnimations.GetRandomElement(), true);
			}
			else
			{
				Vec3 randomDirection = new Vec3(Agent.LookDirection);
				randomDirection.RotateAboutZ(MBRandom.RandomFloat * 2 - 1f);
				Vec3 destinationVec3 = Agent.Position + randomDirection * MBRandom.RandomFloat * 10;
				WorldPosition destination = new WorldPosition(Mission.Current.Scene, destinationVec3);
				Log($"Warg moving from {Agent.Position.AsVec2} to {destination.AsVec2}", LogLevel.Debug);
				Agent.SetScriptedPosition(ref destination, false, Agent.AIScriptedFrameFlags.None);
			}
		}

		private void FleeBehavior()
		{
			WorldPosition worldPosition = Agent.Mission.GetClosestFleePositionForAgent(Agent);
			if (Agent.IsRetreating() && Agent.MovementVelocity.Y <= 0.5f)
			{
				Log($"Fleeing warg is stuck", LogLevel.Debug);
				Vec3 randomDirection = new Vec3(Agent.LookDirection);
				randomDirection.RotateAboutZ(MBRandom.RandomFloat * 2 - 1f);
				Vec3 destinationVec3 = Agent.Position + randomDirection * MBRandom.RandomFloat * 100;
				WorldPosition destination = new WorldPosition(Mission.Current.Scene, destinationVec3);
				Log($"To unstuck, warg moving from {Agent.Position.AsVec2} to {destination.AsVec2}", LogLevel.Debug);
				Agent.SetScriptedPosition(ref destination, false, Agent.AIScriptedFrameFlags.None);
			}
			Agent.Retreat(worldPosition);
			Log($"Warg is fleeing from {Agent.Position.AsVec2} to {worldPosition.AsVec2}", LogLevel.Debug);
		}

		private void ChaseTarget()
		{
			if (_target == null) return;

			float distanceToTarget = (_target.Position - Agent.Position).Length;

			// Determine chase behavior - initialize trajectory on first chase
			if (_chaseTrajectory == default)
			{
				float randomValue = MBRandom.RandomFloat;
				if (randomValue < 0.33f)
				{
					_chaseTrajectory = ChaseTrajectory.Straight;
				}
				else if (randomValue < 0.66f)
				{
					_chaseTrajectory = ChaseTrajectory.CurveLeft;
				}
				else
				{
					_chaseTrajectory = ChaseTrajectory.CurveRight;
				}
			}

			// Set speed depending on distance to target
			if (distanceToTarget > 20f || MBRandom.RandomFloat < 0.4f) // 40% chance to charge, or if far from target
			{
				Agent.SetMaximumSpeedLimit(Agent.Monster.WalkingSpeedLimit * 3f, false); // Charge speed
			}
			else
			{
				Agent.SetMaximumSpeedLimit(Agent.Monster.WalkingSpeedLimit, false); // Slow closing in
			}

			// Adjust facing direction to keep track of the target
			Vec3 directionToTarget = (_target.Position - Agent.Position).NormalizedCopy();
			Vec3 curveOffset = Vec3.Zero;

			// Apply the chosen trajectory to determine the path to target
			switch (_chaseTrajectory)
			{
				case ChaseTrajectory.CurveLeft:
					curveOffset = new Vec3(-directionToTarget.z, 0, directionToTarget.x) * 100f; // Curve left
					break;
				case ChaseTrajectory.CurveRight:
					curveOffset = new Vec3(directionToTarget.z, 0, -directionToTarget.x) * 100f; // Curve right
					break;
				case ChaseTrajectory.Straight:
					curveOffset = Vec3.Zero; // No offset for straight approach
					break;
			}

			// Calculate the desired position
			Vec3 desiredPosition = _target.Position - directionToTarget * 3f + curveOffset;

			// Overshoot to ensure warg gets close enough to engage the target properly
			Vec3 overshootPosition = desiredPosition + directionToTarget * 20f;
			WorldPosition targetPos = overshootPosition.ToWorldPosition();
			Log($"Warg chasing target to {targetPos.GetGroundVec3()} ({_chaseTrajectory})", LogLevel.Debug);
			Agent.SetScriptedPosition(ref targetPos, false, Agent.AIScriptedFrameFlags.None);
		}

		private void AttackTarget()
		{
			if (_target == null) return;

			// Choose attack animation based on movement
			if (Agent.MovementVelocity.Y >= 4)
			{
				// Move slightly beyond the target to make sure the running attack reaches
				Vec3 directionToTarget = (_target.Position - Agent.Position).NormalizedCopy();
				Vec3 overshootPosition = _target.Position + directionToTarget * 2f; // Move 2 meters past the target
				WorldPosition overshootWorldPosition = overshootPosition.ToWorldPosition();
				Agent.SetScriptedPosition(ref overshootWorldPosition, false, Agent.AIScriptedFrameFlags.None);
			}

			Agent.WargAttack();
		}

		private void MaintainDistanceFromTarget()
		{
			if (_target == null) return;

			float distanceToTarget = (_target.Position - Agent.Position).Length;

			// Calculate the direction from the warg to the target
			Vec3 directionToTarget = (_target.Position - Agent.Position).NormalizedCopy();

			// If target is too close, walkback or run away
			if (distanceToTarget < WargConstants.THREAT_RANGE * 0.6f)
			{
				float dotProduct = Vec3.DotProduct(Agent.LookDirection, directionToTarget);
				bool targetInFront = dotProduct > 0.7f;
				// Target is walking, use TP to walkback
				if (_target.MovementVelocity.Y < 3 && distanceToTarget > WargConstants.THREAT_RANGE * 0.4f && targetInFront)
				{
					float proportionalDistance = distanceToTarget + (WargConstants.THREAT_RANGE * 0.6f - distanceToTarget) * 0.25f;
					Vec3 fallbackPosition = _target.Position - (directionToTarget * proportionalDistance);
					WorldPosition fallbackWorldPosition = fallbackPosition.ToWorldPosition();
					// Check if path to destination exist before teleporting to avoid obstacles
					if (Mission.Current.Scene.GetNavigationMeshForPosition(ref fallbackPosition))
					{
						Agent.SetScriptedPosition(ref fallbackWorldPosition, true, Agent.AIScriptedFrameFlags.None);
						Agent.TeleportToPosition(fallbackPosition);
						Agent.SetMovementDirection(directionToTarget.AsVec2);
						AnimationSystem.Instance.PlayAnimation(Agent, WargConstants.WalkBackAnimation, false);
					}
					else
					{
						Log($"No path possible to fallback", LogLevel.Warning);
					}
				}
				// Target is running, run back
				else
				{
					Vec3 fallbackPosition = _target.Position - (directionToTarget * WargConstants.THREAT_RANGE);
					WorldPosition fallbackWorldPosition = fallbackPosition.ToWorldPosition();
					Agent.SetMaximumSpeedLimit(Agent.Monster.WalkingSpeedLimit * 3f, false);
					Agent.SetScriptedPosition(ref fallbackWorldPosition, false, Agent.AIScriptedFrameFlags.None);
				}
			}
			// If threat is too far, slowly close in
			else if (distanceToTarget > WargConstants.THREAT_RANGE * 0.7f)
			{
				Vec3 desiredPosition = _target.Position - (directionToTarget * (WargConstants.THREAT_RANGE * 0.6f));
				WorldPosition desiredWorldPosition = desiredPosition.ToWorldPosition();
				Agent.SetScriptedPosition(ref desiredWorldPosition, true, Agent.AIScriptedFrameFlags.DoNotRun);
			}
			// If threat is at correct distance, chance to play random anim
			else if (MBRandom.RandomFloat < 0.05f)
			{
				Agent.SetMovementDirection(directionToTarget.AsVec2);
				AnimationSystem.Instance.PlayAnimation(Agent, WargConstants.CarefulAnimations.GetRandomElement(), false);
			}
		}
	}
}
