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
				attackDuration = WargConstants.AttackRunningAnimation.MaxDuration;
				startDelay = 0.2f;
			}
			else
			{
				AnimationSystem.Instance.PlayAnimation(warg, WargConstants.AttackAnimation, true);
				attackDuration = WargConstants.AttackAnimation.MaxDuration;
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
						(agent, target) => HandleCollision(target, warg),
						(agent, target) => HandleParriedCollision(target, warg)
					));
				}
			}
		}

		public static void HandleCollision(Agent target, Agent damager)
		{
			if (target == null || !target.IsActive())
			{
				Log("HandleCollision: attempt to target a null or dead agent.", LogLevel.Warning);
				return;
			}

			try
			{
				if (target.State == AgentState.Active || target.State == AgentState.Routed)
				{
					if (target.IsFadingOut())
						return;

					var damagerAgent = damager != null ? damager : target;
					int damage = target.Monster.StringId == "warg" ? 25 : 50; // Wargs do less damage to each other

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
				Log($"Error in HandleCollision for the Warg: {e.Message}", LogLevel.Error);
			}
		}


		public static void HandleParriedCollision(Agent target, Agent damager)
		{
			if (target == null || !target.IsActive())
			{
				Log("HandleParriedCollision: attempt to target a null or dead agent.", LogLevel.Warning);
				return;
			}

			try
			{
				if (target.State == AgentState.Active || target.State == AgentState.Routed)
				{
					if (target.IsFadingOut())
						return;

					// TODO : if target parried with a shield, reduce shield durability, otherwise project target
					var damagerAgent = damager != null ? damager : target;
					CoreUtils.TakeDamage(target, damagerAgent, 0); // Application of the blow.
				}
			}
			catch (Exception e)
			{
				Log($"Error in HandleParriedCollision for the Warg: {e.Message}", LogLevel.Error);
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
