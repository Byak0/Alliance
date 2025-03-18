using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentComponents
{
	/// <summary>
	/// Allow the warg to behave independently. Hunting, chilling, fleeing, etc.
	/// </summary>
	public class WargComponent : AL_DefaultAgentComponent
	{
		// Behavior probabilities, from 0 (never) to 1 (always).
		const float FLEE_WHEN_WOUNDED_PROBABILITY = 0.02f;
		const float IDLE_RANDOM_ANIM_PROBABILITY = 0.3f;
		const float IDLE_RANDOM_MOVE_PROBABILITY = 0.5f;
		const float BE_CAREFUL_PROBABILITY = 0.98f;
		const float ATTACK_WITH_RIDER_PROBABILITY = 0.8f;
		const float ATTACK_PROBABILITY = 0.9f;
		const float START_CHASE_PROBABILITY = 0.25f;
		private Agent _target;
		private Agent _threat;
		private Agent _lastAttacker;
		private float _refreshDelay;
		private float _targetChangeDelay;
		private float _lastAttackDelay;
		private MonsterState _currentState;
		private bool _isWounded;
		private float _fearOfThreat;
		private bool _forcePassiveBehavior;

		public MonsterState CurrentState => _currentState;
		public Agent Target => _target;
		public Agent Threat => _threat;
		public float FearOfThreat => _fearOfThreat;
		public Agent LastAttacker => _lastAttacker;
		public bool IsWounded => _isWounded;

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
			//if (Agent.Team == null)
			//{
			//	if (Agent.RiderAgent?.Team != null)
			//	{
			//		Agent.SetTeam(Agent.RiderAgent.Team, true);
			//	}
			//	else
			//	{
			//		Team randomTeam = MBRandom.RandomInt(0, 2) == 0 ? Mission.Current.AttackerTeam : Mission.Current.DefenderTeam;
			//		Agent.SetTeam(randomTeam, true);
			//	}
			//	Log($"In WargComponent, agent.Team was null ({Agent.Team})", LogLevel.Debug);
			//	return;
			//}
		}

		public override void OnHit(Agent affectorAgent, int damage, in MissionWeapon affectorWeapon)
		{
			if (affectorAgent != null && affectorAgent != Agent.RiderAgent)
			{
				_lastAttacker = affectorAgent;
				ChangeTarget(_lastAttacker);
			}
		}

		public override void OnTick(float dt)
		{
			try
			{
				// Tick once every RefreshTime or CloseRangeRefreshTime if near target
				_refreshDelay += dt;
				_targetChangeDelay += dt;
				_lastAttackDelay += dt;
				_fearOfThreat = Math.Max(0, _fearOfThreat - 0.01f * dt);
				if (_currentState == MonsterState.Attack || (_currentState == MonsterState.Chase || _currentState == MonsterState.Careful) && _target != null && (_target.Position - Agent.Position).Length < WargConstants.THREAT_RANGE)
				{
					if (_refreshDelay <= WargConstants.CLOSE_RANGE_AI_REFRESH_TIME) return;
				}
				else
				{
					if (_refreshDelay <= WargConstants.AI_REFRESH_TIME) return;
				}
				_refreshDelay = 0;

				if (_forcePassiveBehavior && (Agent.RiderAgent == null || Agent.RiderAgent.IsAIControlled))
				{
					RandomIdleBehavior();
				}
				else if (Agent.RiderAgent != null)
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

				if (Agent.RiderAgent == null || Agent.RiderAgent.IsAIControlled)
				{
					switch (_currentState)
					{
						case MonsterState.None: break;
						case MonsterState.Idle:
							RandomIdleBehavior();
							break;
						case MonsterState.Chase:
							ChaseTarget();
							break;
						case MonsterState.Attack:
							AttackTarget();
							break;
						case MonsterState.Flee:
							FleeBehavior();
							break;
						case MonsterState.Careful:
							MaintainDistanceFromThreat();
							break;
					}
				}

			}
			catch (Exception e)
			{
				Log("Exception in WargBehavior : " + e.Message, LogLevel.Error);
			}
		}

		public override void OnMissionResultReady(MissionResult missionResult)
		{
			_currentState = MonsterState.Idle;
			_forcePassiveBehavior = true;
		}

		private void DetermineTargetAndStateWithAIRider()
		{
			// Check if there is enemy close by
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(7f, Agent);
			_target = nearbyAgents.FirstOrDefault(agt =>
				agt != Agent.RiderAgent
				&& !agt.IsWarg()
				&& (agt.Team == null || agt.Team.IsEnemyOf(Agent.RiderAgent.Team)
				&& CloseEnoughForAttack(agt))
			);

			if (_target == null)
			{
				_currentState = MonsterState.None;
				return;
			}

			if (ShouldAttack(ATTACK_WITH_RIDER_PROBABILITY))
			{
				_currentState = MonsterState.Attack;
				return;
			}

			_currentState = MonsterState.None;
		}

		private void DetermineTargetAndState()
		{
			// Chance to flee when wounded
			if (ShouldFlee(FLEE_WHEN_WOUNDED_PROBABILITY))
			{
				_currentState = MonsterState.Flee;
				return;
			}

			// Be careful when nearby unit is dangerous/wield torch
			if (ShouldBeCareful(BE_CAREFUL_PROBABILITY, out Agent _threat))
			{
				ChangeThreat(_threat);
				// If warg is scared of threat, keep distance
				if (_fearOfThreat > 0)
				{
					_currentState = MonsterState.Careful;
					return;
				}
				// If warg is no longer scared, target the threat
				else
				{
					ChangeTarget(_threat);
				}
			}

			// Attack target if possible
			if (ShouldAttack(ATTACK_PROBABILITY))
			{
				_currentState = MonsterState.Attack;
				return;
			}

			// Target someone if no current target
			if (ShouldChaseSomeone(START_CHASE_PROBABILITY, out Agent _bestTarget))
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
				// Tell the target that he is threatened
				if (_target.IsHuman)
				{
					MBList<Agent> targetAllies = new MBList<Agent> { };
					Mission.Current.GetNearbyAllyAgents(_target.Position.AsVec2, 10, _target.Team, targetAllies);
					for (int i = 0; i < targetAllies.Count && i < 3; i++)
					{
						targetAllies[i].GetComponent<HumanoidComponent>()?.SetThreat(Agent);
					}
					_target.GetComponent<HumanoidComponent>()?.SetThreat(Agent);
				}
			}
		}

		private void ChangeThreat(Agent threat)
		{
			if (_threat != threat && threat != null)
			{
				_threat = threat;
				float woundedBonus = (_isWounded ? 0.25f : 0) +
									 (_threat.Health < _threat.HealthLimit * 0.25f ? 0 : 0.25f);
				float raceBonus = 0;
				if (_threat.IsTroll() || _threat.IsEnt()) raceBonus = 5f;
				if (_threat.IsDwarf()) raceBonus = -0.25f;

				// Random initial fear value between 0 and 6
				_fearOfThreat = (0.25f + MBRandom.RandomFloat / 2) + woundedBonus + raceBonus;
			}
		}

		private bool CloseEnoughForAttack(Agent target)
		{
			float distanceToTarget = (target.Position - Agent.Position).Length;
			float distanceForAttack = 2f + Agent.MovementVelocity.Y;
			bool closeEnoughForAttack = distanceToTarget <= distanceForAttack;
			bool frontAttack = AdvancedCombatHelper.IsInFrontCone(Agent, target, 45);

			// Close enough to attack and target is in front
			if (closeEnoughForAttack && frontAttack)
			{
				return true;
			}
			return false;
		}

		private bool ShouldAttack(float probability)
		{
			if (_target == null || _target.Health <= 0 || _lastAttackDelay < WargConstants.ATTACK_COOLDOWN) return false;

			// Close enough to attack and target is in front
			if (MBRandom.RandomFloat < probability && (CloseEnoughForAttack(_target) || _target.MountAgent != null && CloseEnoughForAttack(_target.MountAgent)))
			{
				return true;
			}
			return false;
		}

		private bool ShouldChaseSomeone(float probability, out Agent _bestTarget)
		{
			_bestTarget = null;
			if (_target != null && IsTargetNoLongerRelevant())
			{
				_target = null;
			}
			if (_target == null)
			{
				if (MBRandom.RandomFloat > probability) return false;

				_bestTarget = GetBestTarget(WargConstants.CHASE_RADIUS);
				if (_bestTarget != null)
				{
					ChangeTarget(_bestTarget);
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

		private bool IsTargetNoLongerRelevant()
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
			return _currentState == MonsterState.Flee && MBRandom.RandomFloat < 0.99f || _isWounded && MBRandom.RandomFloat < probability;
		}

		private Agent GetBestTarget(float range)
		{
			List<Agent> nearbyAgents = CoreUtils.GetNearAliveAgentsInRange(range, Agent);

			Agent bestTarget = null;
			float bestScore = float.MinValue;

			foreach (Agent potentialTarget in nearbyAgents)
			{
				if (potentialTarget == Agent || !potentialTarget.IsActive() || (potentialTarget.IsWarg() || potentialTarget.HasMount && potentialTarget.MountAgent.IsWarg()) && MBRandom.RandomFloat < 0.9999f)
					continue;

				float score = 0;

				FormationComponent formationComponent = potentialTarget.MissionPeer?.GetComponent<FormationComponent>();
				if (potentialTarget == _lastAttacker)
					score += 100; // Prefer to hit back its last attacker

				if (formationComponent != null && formationComponent.State == FormationState.Rambo)
					score += 40; // Prefer rambos

				//if (potentialTarget.Team != Agent.Team)
				//	score += 40; // Prefer enemies

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
			return agent != Agent && agent.IsActive() && (IsWieldingTorch(agent) || agent.IsTroll() || agent.IsEnt());
		}

		private void RandomIdleBehavior()
		{
			if (MBRandom.RandomFloat < IDLE_RANDOM_ANIM_PROBABILITY) // 30% chance to play a random animation while idling
			{
				AnimationSystem.Instance.PlayAnimation(Agent, WargConstants.IdleAnimations.GetRandomElement(), false);
			}
			else if (MBRandom.RandomFloat < IDLE_RANDOM_MOVE_PROBABILITY) // 50% chance to move to a random position
			{
				Vec3 randomDirection = new Vec3(Agent.LookDirection, -1f);
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
				Vec3 randomDirection = new Vec3(Agent.LookDirection, -1f);
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
			bool isTargetInFront = AdvancedCombatHelper.IsInFrontCone(Agent, _target, 180);
			if (distanceToTarget < 8 && !isTargetInFront)
			{
				// Just go straight to gain distance
				destination = (Agent.Position + Agent.LookDirection * 100f).ToWorldPosition();
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

		private void MaintainDistanceFromThreat()
		{
			if (_threat == null) return;

			float distanceToTarget = (_threat.Position - Agent.Position).Length;
			// Target should maintain distance towards target relative to its fear
			float desiredRange = WargConstants.THREAT_RANGE * (0.5f + _fearOfThreat);

			// Calculate the direction from the warg to the target
			Vec3 directionToTarget = (_threat.Position - Agent.Position).NormalizedCopy();

			// If target is too close, walkback or run away
			if (distanceToTarget < desiredRange * 0.6f)
			{
				float dotProduct = Vec3.DotProduct(Agent.LookDirection, directionToTarget);
				bool targetInFront = dotProduct > 0.7f;
				// Target is walking, use TP to walkback
				if (_threat.MovementVelocity.Y < 3 && distanceToTarget > desiredRange * 0.4f && targetInFront)
				{
					float proportionalDistance = distanceToTarget + (desiredRange * 0.6f - distanceToTarget) * 0.25f;
					Vec3 fallbackPosition = _threat.Position - directionToTarget * proportionalDistance;
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
					Vec3 fallbackPosition = _threat.Position - directionToTarget * WargConstants.THREAT_RANGE;
					WorldPosition fallbackWorldPosition = fallbackPosition.ToWorldPosition();
					Agent.SetMaximumSpeedLimit(Agent.Monster.WalkingSpeedLimit * 3f, false);
					Agent.SetScriptedPosition(ref fallbackWorldPosition, false, Agent.AIScriptedFrameFlags.None);
				}
			}
			// If threat is too far, slowly close in
			else if (distanceToTarget > desiredRange * 0.7f)
			{
				Vec3 desiredPosition = _threat.Position - directionToTarget * (desiredRange * 0.6f);
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
