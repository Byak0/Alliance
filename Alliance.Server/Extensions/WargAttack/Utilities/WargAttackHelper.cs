using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Server.Core.Utils;
using Alliance.Server.Extensions.WargAttack.Behavior;
using Alliance.Server.Extensions.WargAttack.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.WargAttack.Utilities
{
	public static class WargAttackHelper
	{
		public static void WargAttack(this Agent warg)
		{
			float radius = 10f;
			float impactDistance = 0.1f;
			float attackDuration = 0f;
			float startDelay = 0f;

			List<Agent> nearbyAllAgents = CoreUtils.GetNearAliveAgentsInRange(radius, warg);

			if (warg.MovementVelocity.Y >= 4)
			{
				AnimationSystem.Instance.PlayAnimation(warg, WargConstants.AttackRunningAnimation, true);
				attackDuration = WargConstants.AttackRunningAnimation.MaxDuration - 0.2f;
				startDelay = 0.2f;
			}
			else
			{
				AnimationSystem.Instance.PlayAnimation(warg, WargConstants.AttackAnimation, true);
				attackDuration = WargConstants.AttackAnimation.MaxDuration - 0.1f;
				startDelay = 0.1f;
			}

			foreach (Agent agent in nearbyAllAgents)
			{
				if (agent != null && agent != warg && agent.IsActive() && agent != warg.RiderAgent)
				{
					Mission.Current.AddMissionBehavior(new BoneCheckDuringAnimationBehavior(
						warg,
						agent,
						WargConstants.WargBoneIds,
						startDelay,
						attackDuration,
						impactDistance,
						(agent, target) => HandleTargetHit(target, warg),
						(agent, target) => HandleTargetParried(target, warg)
					));
				}
			}
		}

		public static void HandleTargetHit(Agent target, Agent warg)
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

					// Avoid friendly fire
					if (target.Monster.StringId == "warg" && MBRandom.RandomFloat < 0.9f)
						return;

					int damage = 50;

					// Check if damager is dead
					Agent damagerAgent = warg;
					if (damagerAgent == null || damagerAgent.Health <= 0)
					{
						// If damager is dead, use target as damager and reduce damage
						damagerAgent = target;
						damage = 20;
					}

					if (target.IsMount) damage *= 2; // todo use IsHorse here

					// If target is AI controlled, chance of reverse damage to simulate target defending
					if (target.IsAIControlled && target.IsHuman && MBRandom.RandomFloat < 0.5f)
					{
						// Target deals damage to warg
						if (!target.HasMount)
						{
							target.SetMovementDirection((damagerAgent.Position - target.Position).AsVec2);
							AnimationSystem.Instance.PlayAnimation(target, WargConstants.DefendAnimation, true);
						}
						CoreUtils.TakeDamage(damagerAgent, target, damage);
					}
					else
					{
						// Warg deals damage to target
						CoreUtils.TakeDamage(target, damagerAgent, damage);
					}
				}
			}
			catch (Exception e)
			{
				Log($"Error in HandleTargetHit for the Warg: {e.Message}", LogLevel.Error);
			}
		}


		public static void HandleTargetParried(Agent target, Agent warg)
		{
			if (target == null || !target.IsActive())
			{
				Log("HandleTargetParried: attempt to target a null or dead agent.", LogLevel.Warning);
				return;
			}

			try
			{
				if (target.State == AgentState.Active || target.State == AgentState.Routed)
				{
					if (target.IsFadingOut())
						return;

					// TODO : if target parried with a shield, reduce shield durability, otherwise project target
					var damagerAgent = warg != null ? warg : target;
					CoreUtils.TakeDamage(target, damagerAgent, 0); // Application of the blow.
				}
			}
			catch (Exception e)
			{
				Log($"Error in HandleTargetParried for the Warg: {e.Message}", LogLevel.Error);
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
				|| angleInDegrees > (180 + stanceOffset))
			{
				position = "Back Right";
				backAttack = true;
			}
			else if (angleInDegrees < (0 + stanceOffset) && angleInDegrees > -(90 - stanceOffset))
			{
				position = "Front Right";
			}
			else if (angleInDegrees < (90 + stanceOffset) && angleInDegrees > (0 + stanceOffset))
			{
				position = "Front Left";
			}
			else if (angleInDegrees > (90 + stanceOffset) || angleInDegrees < -(180 - stanceOffset))
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
