using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.Utilities
{
	public static class AdvancedCombatHelper
	{
		public enum BlowDirection
		{
			Front,
			Back,
			Left,
			Right
		}

		/// <summary>
		/// Simple custom kick attack. Works for humans, horses and camels.
		/// </summary>
		/// <param name="agent"></param>
		public static void CustomKick(this Agent agent)
		{
			ActionIndexCache kick;
			List<sbyte> kickBoneIds;
			float targetDetectionRange = 20f;
			float boneCollisionRadius = 0.05f;
			float actionProgressMin = 0f;
			float actionProgressMax = 1f;
			int damage = 5;
			float projectionStrength = 1f;

			if (agent.IsHuman)
			{
				kick = agent.GetIsLeftStance() ? ActionIndexCache.Create("act_kick_right_leg") : ActionIndexCache.Create("act_kick_left_leg");
				kickBoneIds = agent.GetIsLeftStance() ? new List<sbyte> { 7, 8 } : new List<sbyte> { 3, 4 };
				actionProgressMin = 0.4f;
				actionProgressMax = 0.95f;
				projectionStrength = 0.75f;
			}
			else if (agent.IsHorse())
			{
				kick = ActionIndexCache.Create("act_horse_kick");
				kickBoneIds = new List<sbyte> { 20, 25 };
				actionProgressMin = 0.3f;
				actionProgressMax = 0.7f;
				damage = 10;
			}
			else if (agent.IsCamel())
			{
				kick = ActionIndexCache.Create("act_camel_rear");
				kickBoneIds = new List<sbyte> { 11, 17 };
				boneCollisionRadius = 0.1f;
				actionProgressMin = 0.3f;
				actionProgressMax = 0.8f;
				projectionStrength = 0.1f;
			}
			// Invalid race
			else
			{
				Log($"Agent {agent.Name} is not a human, horse or camel. Cannot perform kick attack.", LogLevel.Warning);
				return;
			}

			agent.CustomAttack(kick, kickBoneIds, actionProgressMin, actionProgressMax, targetDetectionRange, boneCollisionRadius, true, (attacker, target, boneId) =>
			{
				attacker.DealDamage(target, damage);
				target.ProjectAgent(attacker.Position, projectionStrength);
			});
		}

		/// <summary>
		/// Custom attack using a specific animation and bones for collision detection.
		/// </summary>
		/// <param name="agent">Agent executing the attack.</param>
		/// <param name="action">Action to play and watch collisions for.</param>
		/// <param name="bonesIdsForCollision">List of bones ID to watch collisions for.</param>
		/// <param name="actionProgressMin">Progress (between 0f-1f) of the action before watching for collisions.</param>
		/// <param name="actionProgressMax">Progress (between 0f-1f)  of the action after which we stop watching for collisions.</param>
		/// <param name="targetDetectionRange">Range around the agent used to extract list of potential targets when watching for collisions.</param>
		/// <param name="boneCollisionRadius">Collision radius for bones.</param>
		/// <param name="stopOnFirstHit">True to stop watching for collisions after the first hit.</param>
		/// <param name="onHitCallback">Action to execute on collision.</param>
		/// <param name="onExpirationCallBack">Optional action on expiration.</param>
		public static void CustomAttack(this Agent agent, ActionIndexCache action, List<sbyte> bonesIdsForCollision, float actionProgressMin, float actionProgressMax, float targetDetectionRange, float boneCollisionRadius, bool stopOnFirstHit, Action<Agent, Agent, sbyte> onHitCallback, Action onExpirationCallBack = null)
		{
			if (agent == null || !agent.IsActive() || agent.IsFadingOut())
			{
				Log("CustomAttack: attempt to use on a null or dead agent.", LogLevel.Warning);
				return;
			}

			// Play the animation
			agent.SetActionChannel(0, action, true);

			// Get potential targets
			List<Agent> targets = CoreUtils.GetNearAliveAgentsInRange(targetDetectionRange, agent).FindAll(agt => agt != agent && agt.RiderAgent != agent && agt.IsActive());
			if (targets.Count == 0) return;

			// Check for collisions
			AdvancedCombatBehavior advancedCombat = Mission.Current.GetMissionBehavior<AdvancedCombatBehavior>();
			advancedCombat.AddBoneCheckComponent(new BoneCheckDuringAnimation(
						action,
						agent,
						targets,
						bonesIdsForCollision,
						actionProgressMin,
						actionProgressMax,
						boneCollisionRadius,
						stopOnFirstHit,
						onHitCallback,
						onExpirationCallBack
					));
		}

		public static void WargAttack(this Agent warg)
		{
			List<sbyte> boneIds;
			float targetDetectionRange = 20f;
			float boneCollisionRadius = 0.3f;
			float actionProgressMax;
			float actionProgressMin;
			ActionIndexCache action;

			if (warg.MovementVelocity.Y >= 4)
			{
				boneIds = new List<sbyte> { 23 };
				action = WargConstants.AttackRunningAnimation;
				actionProgressMin = 0.1f;
				actionProgressMax = 0.7f;
				boneCollisionRadius = 0.4f;
			}
			else
			{
				boneIds = new List<sbyte> { 23, 37, 43 };
				action = WargConstants.AttackAnimation;
				actionProgressMin = 0.1f;
				actionProgressMax = 0.5f;
			}

			warg.CustomAttack(action, boneIds, actionProgressMin, actionProgressMax, targetDetectionRange, boneCollisionRadius, false, (attacker, target, boneId) => HandleWargTargetHit(attacker, target, boneId));
		}

		public static void HandleWargTargetHit(Agent attacker, Agent target, sbyte boneId)
		{
			if (target == null || !target.IsActive())
			{
				Log("HandleTargetHit: attempt to target a null or dead agent.", LogLevel.Warning);
				return;
			}

			try
			{
				if (target.State == AgentState.Active || target.State == AgentState.Routed)
				{
					if (target.IsFadingOut())
						return;

					bool isAIControlled = attacker?.RiderAgent?.IsAIControlled ?? true;

					// Avoid friendly fire if AI controlled
					if ((isAIControlled && (target.IsWarg() || target.HasMount && target.MountAgent.IsWarg())
						&& MBRandom.RandomFloat < 0.99f))
						return;

					// TODO : Retrieve body part from boneId ?
					float damageAbsorption = (100 - target.GetBaseArmorEffectivenessForBodyPart(BoneBodyPartType.Chest)) / 100;
					int damage = (int)(60 * MathF.Clamp(damageAbsorption, 0, 1));

					// Check if warg and its rider are dead
					Agent damagerAgent = attacker?.RiderAgent ?? attacker;
					if (damagerAgent == null || damagerAgent.Health <= 0)
					{
						// If they are, use target as damager and reduce damage (as warg was killed in action)
						damagerAgent = target;
						damage = 20;
					}

					//SendMessageToAll($"{damagerAgent.Name} hitting {target.Name} for {damage} dmg.");

					if (target.IsHorse() || target.IsCamel()) damage *= 2;

					if (!target.HasMount)
					{
						float force = attacker.MovementVelocity.Y >= 4 ? 1f : 0f;
						ProjectAgent(target, damagerAgent.Position, force);
					}

					CoreUtils.TakeDamage(target, damagerAgent, damage);
					Log($"{attacker.RiderAgent?.Name} hitting {target.Index} - collision on bone {boneId}", LogLevel.Debug);
				}
			}
			catch (Exception e)
			{
				Log($"Error in HandleTargetHit for the Warg: {e.Message}", LogLevel.Error);
			}
		}

		// Project an agent using position as starting point
		public static void ProjectAgent(this Agent nearbyAgent, Vec3 position, float force = 1f)
		{
			// TODO handle horses (and wargs ?) well everything without human skeleton
			if (!nearbyAgent.IsHuman || nearbyAgent.HasMount || nearbyAgent.IsTroll() || nearbyAgent.IsEnt()) return;

			// TODO LOCK agents positions during projections (duplicate animation clips and enforce position?)

			// Calculate the direction of the projection
			BlowDirection blowDirection = GetDirectionOfBlow(nearbyAgent, position);

			// Select the animation to play based on the direction and force
			Animation projectionAnimation;
			switch (blowDirection)
			{
				case BlowDirection.Back:
					int animationIndex = MathF.Min(AdvancedCombatConstants.FrontProjectionAnimations.Count - 1, (int)(force * AdvancedCombatConstants.FrontProjectionAnimations.Count));
					projectionAnimation = AdvancedCombatConstants.FrontProjectionAnimations[animationIndex];
					break;
				case BlowDirection.Front:
					animationIndex = MathF.Min(AdvancedCombatConstants.BackProjectionAnimations.Count - 1, (int)(force * AdvancedCombatConstants.BackProjectionAnimations.Count));
					projectionAnimation = AdvancedCombatConstants.BackProjectionAnimations[animationIndex];
					break;
				case BlowDirection.Right:
					animationIndex = MathF.Min(AdvancedCombatConstants.LeftProjectionAnimations.Count - 1, (int)(force * AdvancedCombatConstants.LeftProjectionAnimations.Count));
					projectionAnimation = AdvancedCombatConstants.LeftProjectionAnimations[animationIndex];
					break;
				case BlowDirection.Left:
				default:
					animationIndex = MathF.Min(AdvancedCombatConstants.RightProjectionAnimations.Count - 1, (int)(force * AdvancedCombatConstants.RightProjectionAnimations.Count));
					projectionAnimation = AdvancedCombatConstants.RightProjectionAnimations[animationIndex];
					break;
			}

			AnimationSystem.Instance.PlayAnimation(nearbyAgent, projectionAnimation, true);
			//Log($"{blowDirection} - Playing {projectionAnimation.Name}");
		}

		public static BlowDirection GetDirectionOfBlow(Agent victim, Vec3 blowOrigin)
		{
			Vec3 victimLookDirection = victim.GetMovementDirection().ToVec3();
			// Calculate the vector from the victim to the attacker
			Vec3 victimToBlowOrigin = (blowOrigin - victim.Position).NormalizedCopy();
			float angleInRadians = (float)Math.Atan2(victimLookDirection.x * victimToBlowOrigin.y - victimLookDirection.y * victimToBlowOrigin.x, victimLookDirection.x * victimToBlowOrigin.x + victimLookDirection.y * victimToBlowOrigin.y);
			float angleInDegrees = (float)(angleInRadians * (180f / Math.PI));
			string position = string.Empty;
			if (angleInDegrees < -135 && angleInDegrees > -180)
			{
				position = "Back Right";
				return BlowDirection.Back;
			}
			else if (angleInDegrees < -45 && angleInDegrees > -135)
			{
				position = "Front Right";
				return BlowDirection.Right;
			}
			else if (angleInDegrees < 45 && angleInDegrees > -45)
			{
				position = "Front Left";
				return BlowDirection.Front;
			}
			else if (angleInDegrees > 45 && angleInDegrees < 135)
			{
				position = "Back Left";
				return BlowDirection.Left;
			}
			else
			{
				position = "error ? back left ?";
				return BlowDirection.Back;
			}
		}

		public static bool IsInFrontCone(Agent attacker, Agent victim, float coneAngle)
		{
			// Convert the cone angle to radians for comparison
			float halfConeAngleRadians = coneAngle * 0.5f * (float)(Math.PI / 180);

			// Get the attacker's forward direction as a normalized vector
			Vec3 attackerLookDirection = attacker.GetMovementDirection().ToVec3().NormalizedCopy();

			// Calculate the vector from the attacker to the victim and normalize it
			Vec3 attackerToVictim = (victim.Position - attacker.Position).NormalizedCopy();

			// Calculate the angle in radians between the attacker's forward direction and attackerToVictim vector
			float dotProduct = Vec3.DotProduct(attackerLookDirection, attackerToVictim);
			float angleBetween = (float)Math.Acos(dotProduct);

			// Check if this angle is within the cone angle
			return angleBetween <= halfConeAngleRadians;
		}

		public static bool IsBackAttack(Agent attacker, Agent victim)
		{
			Vec3 victimLookDirection = victim.GetMovementDirection().ToVec3();

			// Calculate the vector from the victim to the attacker
			Vec3 victimToAttacker = (attacker.Position - victim.Position).NormalizedCopy();

			float angleInRadians = (float)Math.Atan2(victimLookDirection.x * victimToAttacker.y - victimLookDirection.y * victimToAttacker.x, victimLookDirection.x * victimToAttacker.x + victimLookDirection.y * victimToAttacker.y);
			float angleInDegrees = (float)(angleInRadians * (180f / Math.PI));

			string position = string.Empty;

			float stanceOffset = victim.GetIsLeftStance() ? 25f : -25f;

			bool backAttack = false;

			if (angleInDegrees < -(90 - stanceOffset) && angleInDegrees > -(180 - stanceOffset)
				|| angleInDegrees > 180 + stanceOffset)
			{
				position = "Back Right";
				backAttack = true;
			}
			else if (angleInDegrees < 0 + stanceOffset && angleInDegrees > -(90 - stanceOffset))
			{
				position = "Front Right";
			}
			else if (angleInDegrees < 90 + stanceOffset && angleInDegrees > 0 + stanceOffset)
			{
				position = "Front Left";
			}
			else if (angleInDegrees > 90 + stanceOffset || angleInDegrees < -(180 - stanceOffset))
			{
				position = "Back Left";
				backAttack = true;
			}
			else
			{
				position = "error ? back left ?";
				backAttack = true;
			}

			Log(position, LogLevel.Debug);
			Log("angleInRadians: " + angleInRadians.ToString(), LogLevel.Debug);
			Log("angleInDegrees: " + angleInDegrees.ToString(), LogLevel.Debug);
			return backAttack;
		}
	}
}
