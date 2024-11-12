using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Server.Core.Utils;
using Alliance.Server.Extensions.WargAttack.Models;
using Alliance.Server.Extensions.WargAttack.Utilities;
using System;
using System.Collections.Generic;
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
		private Agent _lastAttacker;
		private float _refreshDelay;
		private float _targetChangeDelay;
		private float _lastAttackDelay;
		private MonsterState _currentState;
		private bool _isWounded;
		private ChaseTrajectory _chaseTrajectory;
		private float _fearOfTarget;

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

		public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
		{
			if (affectorAgent != null)
			{
				_lastAttacker = affectorAgent;
				ChangeTarget(_lastAttacker);
			}
		}

		public override void OnTickAsAI(float dt)
		{
			try
			{
				// Tick once every RefreshTime or CloseRangeRefreshTime if near target
				_refreshDelay += dt;
				_targetChangeDelay += dt;
				_lastAttackDelay += dt;
				_fearOfTarget = Math.Max(0, _fearOfTarget - (0.01f * dt));
				if (_currentState == MonsterState.Attack || (_currentState == MonsterState.Chase || _currentState == MonsterState.Careful) && _target != null && (_target.Position - Agent.Position).Length < WargConstants.THREAT_RANGE)
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
						Log($"Warg {Agent.Index} is idling", LogLevel.Debug);
						break;
					case MonsterState.Chase:
						ChaseTarget();
						Log($"Warg {Agent.Index} is chasing {_target?.Name} ({_chaseTrajectory})", LogLevel.Debug);
						break;
					case MonsterState.Attack:
						AttackTarget();
						Log($"Warg {Agent.Index} is attacking", LogLevel.Debug);
						break;
					case MonsterState.Flee:
						FleeBehavior();
						Log($"Warg {Agent.Index} is fleeing", LogLevel.Debug);
						break;
					case MonsterState.Careful:
						MaintainDistanceFromTarget();
						Log($"Warg {Agent.Index} is keeping its distance from target {_target?.Name} (fear level : {_fearOfTarget})", LogLevel.Debug);
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
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(5f, Agent);
			_target = nearbyAgents.FirstOrDefault(agt => agt != Agent.RiderAgent && agt.Team != Agent.RiderAgent.Team);

			if (_target == null)
			{
				_currentState = MonsterState.None;
				return;
			}

			if (ShouldAttack(80))
			{
				_currentState = MonsterState.Attack;
				return;
			}

			_currentState = MonsterState.None;
		}

		private void DetermineTargetAndState()
		{
			// 2 % chance to flee when wounded
			if (ShouldFlee(0.02f))
			{
				_currentState = MonsterState.Flee;
				return;
			}

			// 95% chance to be careful when nearby unit is dangerous/wield torch
			if (ShouldBeCareful(0.95f, out Agent _threat))
			{
				ChangeTarget(_threat);
				if (_fearOfTarget > 0)
				{
					_currentState = MonsterState.Careful;
					return;
				}
			}

			// 90% chance of attacking target if possible
			if (ShouldAttack(0.9f))
			{
				_currentState = MonsterState.Attack;
				return;
			}

			// 25% chance of targetting someone if no current target
			if (ShouldChaseSomeone(0.25f, out Agent _bestTarget))
			{
				ChangeTarget(_bestTarget);
				_currentState = MonsterState.Chase;
				return;
			}

			// Idle if nothing else
			_currentState = MonsterState.Idle;
		}

		private void ChangeTarget(Agent target)
		{
			if (_target != target && target != null)
			{
				_target = target;
				_targetChangeDelay = 0;
				// Random initial fear value between 0.25 and 1
				_fearOfTarget = 0.25f + MBRandom.RandomFloat / 2 + (_isWounded ? 0.25f : 0);
			}
		}

		private bool ShouldAttack(float probability)
		{
			if (_target == null || _target.Health <= 0 || _lastAttackDelay < WargConstants.ATTACK_COOLDOWN) return false;

			float distanceToTarget = (_target.Position - Agent.Position).Length;
			float distanceForAttack = 1.5f + (Agent.MovementVelocity.Y / 1.5f);
			bool closeEnoughForAttack = distanceToTarget <= distanceForAttack;
			bool frontAttack = WargAttackHelper.IsInFrontCone(Agent, _target, 45);

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
			if (_target != null && TargetIsNoLongerRelevant())
			{
				_target = null;
			}
			if (_target == null)
			{
				if (MBRandom.RandomFloat > probability) return false;

				_bestTarget = GetBestTarget(WargConstants.CHASE_RADIUS);
				if (_bestTarget != null)
				{
					_target = _bestTarget;
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
			return (_currentState == MonsterState.Flee && MBRandom.RandomFloat < 0.99f) || _isWounded && MBRandom.RandomFloat < probability;
		}

		private Agent GetBestTarget(float range)
		{
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(range, Agent);

			Agent bestTarget = null;
			float bestScore = float.MinValue;

			foreach (Agent potentialTarget in nearbyAgents)
			{
				if (potentialTarget == Agent || !potentialTarget.IsActive() || ((potentialTarget.Monster.StringId == "warg" || potentialTarget.MountAgent?.Monster.StringId == "warg") && MBRandom.RandomFloat < 0.9999f))
					continue;

				float score = 0;

				FormationComponent formationComponent = potentialTarget.MissionPeer?.GetComponent<FormationComponent>();
				if (potentialTarget == _lastAttacker)
					score += 100; // Prefer to hit back its last attacker

				if (formationComponent != null && formationComponent.State == FormationState.Rambo)
					score += 40; // Prefer rambos

				if (potentialTarget.Team != Agent.Team)
					score += 40; // Prefer enemies

				if (potentialTarget.IsMount && potentialTarget.RiderAgent == null) // TODO : replace IsMount by IsHorse when available
					score += 80; // Prefer riderless horses

				if (potentialTarget.Health / potentialTarget.HealthLimit < WargConstants.LOW_HEALTH_THRESHOLD)
					score += 20; // Prefer wounded targets

				if (IsThreat(potentialTarget))
					score -= 200; // Avoid dangerous targets

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
			return potentialTarget.IsHuman && (
				potentialTarget.WieldedWeapon.Item != null
					&& potentialTarget.WieldedWeapon.Item.StringId.ToLower().Contains("torch")
				|| potentialTarget.WieldedOffhandWeapon.Item != null
					&& potentialTarget.WieldedOffhandWeapon.Item.StringId.ToLower().Contains("torch"));
		}

		private Agent NearbyThreat()
		{
			MBList<Agent> nearbyAgents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(Agent.Position.AsVec2, WargConstants.THREAT_RANGE, nearbyAgents);

			foreach (Agent agent in nearbyAgents)
			{
				if (IsThreat(agent)) return agent;
			}

			return null;
		}

		private bool IsThreat(Agent agent)
		{
			return agent != Agent && agent.IsActive() && IsWieldingTorch(agent);
		}

		private void RandomIdleBehavior()
		{
			if (MBRandom.RandomFloat < 0.3f) // 30% chance to play a random animation while idling
			{
				AnimationSystem.Instance.PlayAnimation(Agent, WargConstants.IdleAnimations.GetRandomElement(), false);
			}
			else if (MBRandom.RandomFloat < 0.5f) // 50% chance to move to a random position
			{
				Vec3 randomDirection = new Vec3(Agent.LookDirection);
				randomDirection.RotateAboutZ(MBRandom.RandomFloat * 2 - 1f);
				Vec3 destinationVec3 = Agent.Position + randomDirection * MBRandom.RandomFloat * 10;
				WorldPosition destination = new WorldPosition(Mission.Current.Scene, destinationVec3);
				Agent.SetScriptedPosition(ref destination, false, Agent.AIScriptedFrameFlags.None);
			}
		}

		private void FleeBehavior()
		{
			WorldPosition worldPosition = Agent.Mission.GetClosestFleePositionForAgent(Agent);
			if (Agent.IsRetreating() && Agent.MovementVelocity.Y <= 0.5f)
			{
				Vec3 randomDirection = new Vec3(Agent.LookDirection);
				randomDirection.RotateAboutZ(MBRandom.RandomFloat * 2 - 1f);
				Vec3 destinationVec3 = Agent.Position + randomDirection * MBRandom.RandomFloat * 100;
				WorldPosition destination = new WorldPosition(Mission.Current.Scene, destinationVec3);
				Log($"Possible missing flee points on map, warg is stuck. Forcing random move from {Agent.Position.AsVec2} to {destination.AsVec2}", LogLevel.Warning);
				Agent.SetScriptedPosition(ref destination, false, Agent.AIScriptedFrameFlags.None);
			}
			Agent.Retreat(worldPosition);
		}

		private void ChaseTarget()
		{
			if (_target == null) return;

			float distanceToTarget = (_target.Position - Agent.Position).Length;

			Agent.SetMaximumSpeedLimit(Agent.Monster.WalkingSpeedLimit * 5f, false); // Charge speed

			Vec3 estimatedTargetDestination = _target.Position + (_target.GetMovementDirection() * _target.MovementVelocity.Y * 1).ToVec3();
			Vec3 directionToEstimatedTarget = (estimatedTargetDestination - Agent.Position).NormalizedCopy();
			Vec3 positionToChase = estimatedTargetDestination + directionToEstimatedTarget * 5f;

			WorldPosition destination = positionToChase.ToWorldPosition();

			// If target is behind the warg, get some distance before turning around
			bool isTargetInFront = WargAttackHelper.IsInFrontCone(Agent, _target, 180);
			if (distanceToTarget < 8 && !isTargetInFront)
			{
				// Just go straight to gain distance
				destination = (Agent.Position + (Agent.LookDirection * 100f)).ToWorldPosition();
			}

			Agent.SetScriptedPosition(ref destination, false, Agent.AIScriptedFrameFlags.None);
			return;
		}

		private void AttackTarget()
		{
			if (_target == null) return;

			// Turn towards target
			Agent.SetMovementDirection((_target.Position - Agent.Position).AsVec2);

			Agent.WargAttack();

			_lastAttackDelay = 0;
		}

		private void MaintainDistanceFromTarget()
		{
			if (_target == null) return;

			float distanceToTarget = (_target.Position - Agent.Position).Length;
			// Target should maintain distance towards target relative to its fear
			float desiredRange = WargConstants.THREAT_RANGE * (0.5f + _fearOfTarget);

			// Calculate the direction from the warg to the target
			Vec3 directionToTarget = (_target.Position - Agent.Position).NormalizedCopy();

			// If target is too close, walkback or run away
			if (distanceToTarget < desiredRange * 0.6f)
			{
				float dotProduct = Vec3.DotProduct(Agent.LookDirection, directionToTarget);
				bool targetInFront = dotProduct > 0.7f;
				// Target is walking, use TP to walkback
				if (_target.MovementVelocity.Y < 3 && distanceToTarget > desiredRange * 0.4f && targetInFront)
				{
					float proportionalDistance = distanceToTarget + (desiredRange * 0.6f - distanceToTarget) * 0.25f;
					Vec3 fallbackPosition = _target.Position - (directionToTarget * proportionalDistance);
					WorldPosition fallbackWorldPosition = fallbackPosition.ToWorldPosition();
					// Check if path to destination exist before teleporting to avoid obstacles
					if (Mission.Current.Scene.GetNavigationMeshForPosition(ref fallbackPosition))
					{
						Agent.SetScriptedPosition(ref fallbackWorldPosition, true, Agent.AIScriptedFrameFlags.None);
						Agent.TeleportToPosition(fallbackPosition);
						Agent.SetMovementDirection(directionToTarget.AsVec2);
					}
					else
					{
						Log($"No path possible for fallback", LogLevel.Warning);
					}
				}
				// Target is running, run away
				else
				{
					Vec3 fallbackPosition = _target.Position - (directionToTarget * WargConstants.THREAT_RANGE);
					WorldPosition fallbackWorldPosition = fallbackPosition.ToWorldPosition();
					Agent.SetMaximumSpeedLimit(Agent.Monster.WalkingSpeedLimit * 3f, false);
					Agent.SetScriptedPosition(ref fallbackWorldPosition, false, Agent.AIScriptedFrameFlags.None);
				}
			}
			// If threat is too far, slowly close in
			else if (distanceToTarget > desiredRange * 0.7f)
			{
				Vec3 desiredPosition = _target.Position - (directionToTarget * (desiredRange * 0.6f));
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
