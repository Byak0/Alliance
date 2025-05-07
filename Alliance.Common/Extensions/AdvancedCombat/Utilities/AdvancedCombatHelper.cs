using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Models;
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
						target.ProjectAgent(damagerAgent.Position, force);
					}

					CoreUtils.TakeDamage(target, damagerAgent, damage);
				}
			}
			catch (Exception e)
			{
				Log($"Error in HandleTargetHit for the Warg: {e.Message}", LogLevel.Error);
			}
		}
	}
}
