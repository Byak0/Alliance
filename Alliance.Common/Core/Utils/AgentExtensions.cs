using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using Alliance.Common.Extensions.AdvancedCombat.Models;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Extensions.AdvancedCombat.Utilities.AdvancedCombatHelper;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Utils
{
	public static class AgentExtensions
	{
		private static readonly int _trollRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("troll");
		private static readonly int _ologRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("olog");
		private static readonly int _olog2RaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("olog2");
		private static readonly int _entRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("ent");
		private static readonly int _dwarfRaceId = TaleWorlds.Core.FaceGen.GetRaceOrDefault("dwarf");

		public static void DealDamage(this Agent agent, Agent victim, int damage, float magnitude = 50f, bool knockDown = false)
		{
			CoreUtils.TakeDamage(victim, agent, damage, magnitude, knockDown);
		}

		// Project an agent using position as starting point
		public static void ProjectAgent(this Agent nearbyAgent, Vec3 position, float force = 1f)
		{
			// TODO handle horses (and wargs ?) well everything without human skeleton
			if (!nearbyAgent.IsHuman || nearbyAgent.HasMount || nearbyAgent.IsTroll() || nearbyAgent.IsEnt()) return;

			// TODO LOCK agents positions during projections (duplicate animation clips and enforce position?)

			// Calculate the direction of the projection
			BlowDirection blowDirection = CoreUtils.GetDirectionOfBlow(nearbyAgent, position);

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

		public static bool IsInFrontCone(this Agent victim, Agent attacker, float coneAngle)
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

		public static bool IsAttackingFromBack(this Agent attacker, Agent victim)
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

		public static bool IsTroll(this Agent agent)
		{
			return agent.Character?.Race == _trollRaceId || agent.Character?.Race == _ologRaceId || agent.Character?.Race == _olog2RaceId;
		}

		public static bool IsEnt(this Agent agent)
		{
			return agent.Character?.Race == _entRaceId;
		}

		public static bool IsDwarf(this Agent agent)
		{
			return agent.Character?.Race == _dwarfRaceId;
		}

		public static bool IsWarg(this Agent agent)
		{
			return agent.Monster.StringId == "warg";
		}

		public static bool IsHorse(this Agent agent)
		{
			return agent.Monster.StringId == "horse";
		}

		public static bool IsCamel(this Agent agent)
		{
			return agent.Monster.StringId == "camel";
		}
	}
}
