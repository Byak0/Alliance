using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Mission;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// Combat related patches.
	/// Credits to Xorberax for original Cut Through Everyone mod.
	/// </summary>
	public static class Patch_AdvancedCombat
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_AdvancedCombat));

		private static bool _patched;

		private static Dictionary<Agent, float> _lastProjectionTimes = new Dictionary<Agent, float>();

		const float MIN_PROJECTION_DELAY = 0.20f; // Minimum delay between consecutive projections (in seconds)
		const float GRAVITY = 9.8f; // Adjust the gravity magnitude as needed
		const float CARPET_WIDTH = 1.5f; // Adjust the width of the flying carpet as needed
		const float PLANE_HEIGHT_OFFSET = 0.5f; // Adjust the height of the flying carpet as needed
		const float PLANE_VECTOR_OFFSET = -0.5f; // Adjust the offset to spawn the carpet in front of the agent as needed
		static readonly ActionIndexCache fallbackAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise");
		static readonly List<BoneBodyPartType> strongParts = new() { BoneBodyPartType.Head, BoneBodyPartType.ShoulderLeft, BoneBodyPartType.ShoulderRight, BoneBodyPartType.ArmLeft, BoneBodyPartType.ArmRight, BoneBodyPartType.Legs };

		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				Harmony.Patch(
					AccessTools.Method(typeof(Mission), "DecideWeaponCollisionReaction"),
					postfix: new HarmonyMethod(typeof(Patch_AdvancedCombat), nameof(DecideWeaponCollisionReactionPostfix))
				);

				Harmony.Patch(
					AccessTools.Method(typeof(Mission), "MeleeHitCallback"),
					postfix: new HarmonyMethod(typeof(Patch_AdvancedCombat), nameof(MeleeHitCallbackPostfix))
				);

				Harmony.Patch(
					AccessTools.Method(typeof(Mission), "GetAttackCollisionResults"),
					postfix: new HarmonyMethod(typeof(Patch_AdvancedCombat), nameof(Postfix_GetAttackCollisionResults))
				);

				Harmony.Patch(
					AccessTools.Method(typeof(Mission), "HandleMissileCollisionReaction"),
					prefix: new HarmonyMethod(typeof(Patch_AdvancedCombat), nameof(Prefix_HandleMissileCollisionReaction))
				);

				return true;
			}
			catch (Exception e)
			{
				Log("Error in Patch_AdvancedCombat: " + e.ToString(), LogLevel.Error);
				return false;
			}
		}

		public static void LatePatch()
		{
			Harmony.Patch(
				AccessTools.Method(typeof(Mission), "MissileHitCallback"),
				prefix: new HarmonyMethod(typeof(Patch_AdvancedCombat), nameof(Prefix_MissileHitCallback))
			);
		}

		// Make arrow bounce
		public static void Prefix_MissileHitCallback(ref int extraHitParticleIndex, ref AttackCollisionData collisionData, Vec3 missileStartingPosition, Vec3 missilePosition, Vec3 missileAngularVelocity, Vec3 movementVelocity, MatrixFrame attachGlobalFrame, MatrixFrame affectedShieldGlobalFrame, int numDamagedAgents, Agent attacker, Agent victim, GameEntity hitEntity)
		{
			if (victim == null) return;

			if (victim.IsTroll())
			{
				bool backAttack = IsBackAttack(attacker, victim);
				bool attackBounced = false;
				List<BoneBodyPartType> strongParts = new List<BoneBodyPartType>() {
					BoneBodyPartType.Head,
					BoneBodyPartType.ShoulderLeft,
					BoneBodyPartType.ShoulderRight,
					BoneBodyPartType.ArmLeft,
					BoneBodyPartType.ArmRight,
					BoneBodyPartType.Legs
				};

				if (backAttack || strongParts.Contains(collisionData.VictimHitBodyPart))
				{
					if (!(MBRandom.RandomInt(1, 10) == 1))
					{
						attackBounced = true;
					}
				}

				if (attackBounced)
				{
					int physicsMaterialIndex = PhysicsMaterial.GetFromName("stone").Index;
					collisionData = SetAttackCollisionData(collisionData, false, false, false, true, physicsMaterialIndex, true, CombatCollisionResult.None);

					//Current.MakeSound(CombatSoundContainer.SoundCodeMissionCombatThrowingStoneMed, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index, ref soundEventParam);
					Current.MakeSound(CombatSoundContainer.SoundCodeMissionCombatThrowingStoneMed, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index);
					//AudioPlayer.Instance.Play("voice_lotr_troll-01.mp3", 1f);
					//Current.MakeSoundOnlyOnRelatedPeer(CombatSoundContainer.SoundCodeMissionCombatThrowingStoneMed, attacker.Position, attacker.Index);
					SoundEventParameter soundEventParameter = new SoundEventParameter("Force", 1f);
					Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsArrowlikeDefault, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index, ref soundEventParameter);
					//SoundEventParameter soundEventParameter = new SoundEventParameter("Force", 1f);
					//Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsArrowlikeDefault, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index, ref soundEventParameter);
				}
			}
			else if (victim.IsEnt())
			{
				int physicsMaterialIndex = PhysicsMaterial.GetFromName("siege_engines").Index;
				collisionData = SetAttackCollisionData(collisionData, false, false, false, true, physicsMaterialIndex, true, CombatCollisionResult.None);

				Current.MakeSound(CombatSoundContainer.SoundCodeMissionCombatWoodShieldBash, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index);
				SoundEventParameter soundEventParameter = new SoundEventParameter("Force", 1f);
				Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsArrowlikeDefault, collisionData.CollisionGlobalPosition, false, false, attacker.Index, victim.Index, ref soundEventParameter);
			}
		}

		// Make arrow bounce
		public static void Prefix_HandleMissileCollisionReaction(Mission __instance, ref int missileIndex, ref MissileCollisionReaction collisionReaction, ref MatrixFrame attachLocalFrame, ref bool isAttachedFrameLocal, ref Agent attackerAgent, ref Agent attachedAgent, ref bool attachedToShield, ref sbyte attachedBoneIndex, ref MissionObject attachedMissionObject, ref Vec3 bounceBackVelocity, ref Vec3 bounceBackAngularVelocity, ref int forcedSpawnIndex)
		{
			//Vec3 zero = Vec3.Zero;
			//Vec3 zero2 = Vec3.Zero;
			//Missile missile = __instance._missiles[missileIndex];
			//missile.CalculateBounceBackVelocity(missileAngularVelocity, collisionData, out zero, out zero2);
			//Determine victim look direction
			//if (attachedAgent != null)
			//{
			//	Vec3 victimLookDirection = attachedAgent.GetMovementDirection().ToVec3();

			//	Calculate the vector from the victim to the attacker
			//   Vec3 victimToAttacker = (attackerAgent.Position - attachedAgent.Position).NormalizedCopy();

			//	float angleInRadians = (float)Math.Atan2(victimLookDirection.x * victimToAttacker.y - victimLookDirection.y * victimToAttacker.x, victimLookDirection.x * victimToAttacker.x + victimLookDirection.y * victimToAttacker.y);
			//	float angleInDegrees = (float)(angleInRadians * (180f / Math.PI));

			//	string position = string.Empty;

			//	float stanceOffset = attachedAgent.GetIsLeftStance() ? 25f : -25f;

			//	bool backAttack = false;

			//	if (angleInDegrees < -(90 - stanceOffset) && angleInDegrees > -(180 - stanceOffset)
			//		|| angleInDegrees > (180 + stanceOffset))
			//	{
			//		position = "Back Right";
			//		backAttack = true;
			//	}
			//	else if (angleInDegrees < (0 + stanceOffset))
			//	{
			//		position = "Front Right";
			//	}
			//	else if (angleInDegrees < (90 + stanceOffset))
			//	{
			//		position = "Front Left";
			//	}
			//	else if (angleInDegrees > (90 + stanceOffset) || angleInDegrees < -(180 - stanceOffset))
			//	{
			//		position = "Back Left";
			//		backAttack = true;
			//	}

			//	if (backAttack)
			//	{
			//		collisionReaction = MissileCollisionReaction.BecomeInvisible;
			//		forcedSpawnIndex = -1;
			//		isAttachedFrameLocal = false;
			//		attachedAgent = null;
			//	}
			//}
		}

		public static void Postfix_GetAttackCollisionResults(ref CombatLogData __result, Agent attackerAgent, Agent victimAgent, GameEntity hitObject, float momentumRemaining, in MissionWeapon attackerWeapon, bool crushedThrough, bool cancelDamage, bool crushedThroughWithoutAgentCollision, ref AttackCollisionData attackCollisionData, ref WeaponComponentData shieldOnBack, ref CombatLogData combatLog)
		{
			if (victimAgent != null && victimAgent.IsTroll() && !attackerAgent.IsEnt() && !attackerAgent.IsTroll())
			{
				bool backAttack = IsBackAttack(attackerAgent, victimAgent);
				bool criticalHit = false;
				List<BoneBodyPartType> strongParts = new List<BoneBodyPartType>() {
					BoneBodyPartType.Head,
					BoneBodyPartType.ShoulderLeft,
					BoneBodyPartType.ShoulderRight,
					BoneBodyPartType.ArmLeft,
					BoneBodyPartType.ArmRight,
					BoneBodyPartType.Legs
				};

				if (backAttack || strongParts.Contains(attackCollisionData.VictimHitBodyPart))
				{
					if (MBRandom.RandomFloat <= 0.01f)
					{
						criticalHit = true;
					}
				}

				if (backAttack)
				{
					if (criticalHit)
					{
						attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 0.25f);
						Log("Critical damage to the back : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
					}
					else
					{
						attackCollisionData.InflictedDamage = Math.Max(attackCollisionData.InflictedDamage - 70, 0);
						Log("Couldn't pierce troll back... : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
					}
				}
				else
				{
					switch (attackCollisionData.VictimHitBodyPart)
					{
						case BoneBodyPartType.Head:
							if (attackCollisionData.CollisionGlobalPosition.Distance(victimAgent.GetEyeGlobalPosition()) < 0.1f)
							{
								attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 3f);
								Log("Critical damage to the head !!! " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							else
							{
								attackCollisionData.InflictedDamage = Math.Max(attackCollisionData.InflictedDamage - 50, 0);
								Log("Low damage to the head : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							break;
						case BoneBodyPartType.Neck:
							attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1.5f);
							Log("Critical damage to the neck : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							break;
						case BoneBodyPartType.ShoulderLeft:
						case BoneBodyPartType.ShoulderRight:
							if (criticalHit)
							{
								attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1f);
								Log("Critical damage to the shoulder : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							else
							{
								attackCollisionData.InflictedDamage = Math.Max(attackCollisionData.InflictedDamage - 50, 0);
								Log("Low damage to the shoulder : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							break;
						case BoneBodyPartType.ArmLeft:
						case BoneBodyPartType.ArmRight:
							if (criticalHit)
							{
								attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1f);
								Log("Critical damage to the arm : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							else
							{
								attackCollisionData.InflictedDamage = Math.Max(attackCollisionData.InflictedDamage - 40, 0);
								Log("Low damage to the arm : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							break;
						case BoneBodyPartType.Legs:
							if (criticalHit)
							{
								attackCollisionData.InflictedDamage = (int)Math.Round(attackCollisionData.InflictedDamage * 1f);
								Log("Critical damage to the leg : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							else
							{
								attackCollisionData.InflictedDamage = Math.Max(attackCollisionData.InflictedDamage - 40, 0);
								Log("Low damage to the leg : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							}
							break;
						default:
							Log("Normal damage to the body : " + attackCollisionData.InflictedDamage, LogLevel.Debug);
							break;
					}
				}
			}
		}

		public static float ToDegrees(float radians)
		{
			return radians * (180f / MathF.PI);
		}

		public static void DecideWeaponCollisionReactionPostfix(ref AttackCollisionData collisionData, Agent attacker, Agent defender, ref MeleeCollisionReaction colReaction)
		{
			bool attackerIsTroll = attacker.IsTroll();
			if (attackerIsTroll)
			{
				//if (defender.HasShieldCached && defender.CurrentGuardMode > Agent.GuardMode.None)
				//{
				//	MissionWeapon shield = defender.Equipment[defender.GetWieldedItemIndex(Agent.HandIndex.OffHand)];

				//	collisionData.IsShieldBroken = true;
				//}
				colReaction = MeleeCollisionReaction.SlicedThrough;
				if (!collisionData.AttackBlockedWithShield && collisionData.CollisionResult == CombatCollisionResult.Parried)
				{
					//collisionData = SetAttackCollisionResult(collisionData, CombatCollisionResult.StrikeAgent);
					colReaction = MeleeCollisionReaction.SlicedThrough;
					Log(defender?.Name + " parried.", LogLevel.Debug);
				}
				if (collisionData.AttackBlockedWithShield || collisionData.CollisionResult == CombatCollisionResult.Blocked)
				{
					//collisionData = SetAttackCollisionResult(collisionData, CombatCollisionResult.StrikeAgent);
					colReaction = MeleeCollisionReaction.Staggered;
					//collisionData.InflictedDamage *= 2;
					Log($"{defender?.Name} blocked, damage={collisionData.InflictedDamage}", LogLevel.Debug);
				}
			}

			if (defender?.IsTroll() ?? false)
			{
				// Check if attacks comes from back etc. to make simulate troll defenses

				// Calculate the angle between the attack direction and the troll's forward direction
				Vec3 weaponBlowDirNormalized = collisionData.WeaponBlowDir.NormalizedCopy();
				Vec3 victimAgentMovementVelocityNormalized = defender.LookDirection.NormalizedCopy();

				float angle = MathF.Acos(Vec3.DotProduct(weaponBlowDirNormalized, victimAgentMovementVelocityNormalized));

				// Define a threshold angle to determine if the attack has hit the troll's back
				float backAngleThreshold = MathF.PI / 2; // Adjust this threshold as needed

				// Check if the attack has hit the troll's back based on the angle
				bool backAttack = angle > backAngleThreshold;

				if (backAttack)
				{
					// Handle attack hitting the troll's back
					colReaction = MeleeCollisionReaction.Bounced;
					//Logger.Log("Back attack -> bounced", color: ConsoleColor.DarkMagenta);                    
				}
				else
				{
					// Handle attack hitting other body parts of the troll
					colReaction = MeleeCollisionReaction.Stuck;
					//Logger.Log("Front attack -> stuck", color: ConsoleColor.Magenta);
				}
			}
			else if (defender?.IsEnt() ?? false)
			{
				colReaction = MeleeCollisionReaction.Bounced;
			}
		}

		public static void MeleeHitCallbackPostfix(ref AttackCollisionData collisionData, Agent attacker, Agent victim, GameEntity realHitEntity, ref float inOutMomentumRemaining, ref MeleeCollisionReaction colReaction, CrushThroughState crushThroughState, Vec3 blowDir, Vec3 swingDir, ref HitParticleResultData hitParticleResultData, bool crushedThroughWithoutAgentCollision)
		{
			int num = collisionData.InflictedDamage + collisionData.AbsorbedByArmor;
			bool flag = num >= 1 && attacker.IsTroll() && !victim.IsTroll();
			if (flag)
			{
				float num2 = 1f;//(float)collisionData.InflictedDamage / num;
				float pushDistance = 3f * num2; // Adjust the push distance as needed
				float projectionDuration = 0.4f * num2; // Adjust the duration of the projection effect

				// Find a way to prevent mounts going to the moon...
				//if (victim.IsMount)
				//{
				//    pushDistance = 2f;
				//    projectionDuration = 1f;
				//}

				// Calculate the push direction based on the blow direction
				Vec3 pushDirection = swingDir;
				Vec3 pushVector = pushDirection * pushDistance + Vec3.Up * 1f;

				if (CanProject(attacker))
				{
					ProjectVictim(attacker, victim, victim.Position - swingDir, projectionDuration);

					// Update the last projection time for the attacker
					UpdateLastProjectionTime(attacker);
				}

				// Remaining momentum
				inOutMomentumRemaining = num2 * 0.8f;
			}
		}

		public static bool CanProject(Agent attacker)
		{
			float currentTime = Current.CurrentTime;

			if (_lastProjectionTimes.TryGetValue(attacker, out float lastProjectionTime))
			{
				// Check if enough time has passed since the last projection
				if (currentTime - lastProjectionTime < 0.05f/*MinProjectionDelay*/)
					return false;
			}

			return true;
		}

		public static void UpdateLastProjectionTime(Agent attacker)
		{
			float currentTime = Current.CurrentTime;

			if (_lastProjectionTimes.ContainsKey(attacker))
				_lastProjectionTimes[attacker] = currentTime;
			else
				_lastProjectionTimes.Add(attacker, currentTime);
		}

		/// <summary>
		/// Project a victim on a vector by simulating a plane entity physically pushing it. 
		/// Agents hit on the victim traject will also suffect a blow.
		/// </summary>
		public static async Task ProjectVictim(Agent attacker, Agent victim, Vec3 projectionOrigin, float projectionDuration)
		{
			float startTime = Current.CurrentTime;

			// test à base de impulse 
			//GameEntity victimEntity = victim.AgentVisuals.GetEntity();
			//victimEntity.AddPhysics(60, Vec3.One, PhysicsShape.GetFromResource("bo_axe_short", false), Vec3.One, Vec3.One, PhysicsMaterial.GetFromName("adobe"), false, 1000);
			//if (victimEntity.HasPhysicsBody())
			//{
			//	victimEntity.ApplyLocalImpulseToDynamicBody(victim.GetChestGlobalPosition(), projectionVector * 100f);
			//}

			// List of all agents already hit by the projecting plane or the victim
			List<Agent> collidedAgents = new List<Agent> { victim };

			//Vec3 normalizedProjVector = projectionVector.NormalizedCopy();
			//GameEntity projectingPlane = GameEntity.Instantiate(Current.Scene, "script_hit_plane", false);

			// Calculate plane original position, based on victim position and offsets defined
			//Vec3 originalPosition = victim.Position + Vec3.Up * 0f/*planeHeightOffset*/ + normalizedProjVector * PLANE_VECTOR_OFFSET;
			// Calculate the rotation to align the carpet with the push vector
			//MatrixFrame originalFrame = CalculateAlignedFrame(originalPosition, projectionVector);
			//originalFrame.Scale(new Vec3(2f, 2f, 2f));

			// Set the frame (position and orientation) of the projecting plane
			//projectingPlane.SetGlobalFrame(originalFrame);

			float projectionForce = 2f - (victim.Position.Distance(projectionOrigin) / 10f);
			AdvancedCombatHelper.ProjectAgent(victim, projectionOrigin, projectionForce);

			while (Current.CurrentTime - startTime < projectionDuration)
			{
				List<Agent> collidingAgents = Current.GetNearbyAgents(
					victim.Position.AsVec2,
					2f,
					new MBList<Agent>()
				).FindAll(agent => !collidedAgents.Contains(agent) && !agent.IsTroll() && victim.Position.Z - agent.Position.Z < 2f);

				// Add agents hit to collided list to ignore them next time
				collidedAgents.AddRange(collidingAgents);

				// Do a blow to every colliding agents
				foreach (Agent agent in collidingAgents)
				{
					//victim.DealDamage(agent, 5);	
					projectionForce = 2f - (agent.Position.Distance(projectionOrigin) / 10f);
					AdvancedCombatHelper.ProjectAgent(agent, projectionOrigin, projectionForce);
					attacker.DealDamage(agent, (int)(projectionForce * 20));
				}

				//float elapsedSeconds = Current.CurrentTime - startTime;
				//float t = elapsedSeconds / projectionDuration;
				//float lerpT = SmoothStep(0f, 1f, t);
				//float currentDistance = projectionVector.Length * lerpT;

				// Calculate the new position without gravity
				//Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

				// Apply gravity to the newPosition
				//float gravityOffset = 0f;// 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
				//newPosition += -Vec3.Up * gravityOffset;

				// Move the projecting plane entity to the new position
				//projectingPlane.SetLocalPosition(newPosition + Vec3.Up * PLANE_HEIGHT_OFFSET);

				// Add a delay to refresh around 50 times per second
				await Task.Delay(20);
			}


			//victim.SetActionChannel(0, fallbackAction, true, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);

			Log("Projection ended after hitting " + collidedAgents.Count + " agents. Original pos : " + projectionOrigin, LogLevel.Debug);

			// Remove the plane entity
			//projectingPlane.Remove(0);
		}

		/// <summary>
		/// Project a victim on a vector by simulating a plane entity physically pushing it. 
		/// Agents hit on the victim traject will also suffect a blow.
		/// </summary>
		public static async Task OldProjectVictim(Agent victim, Vec3 projectionVector, float projectionDuration)
		{
			float startTime = Current.CurrentTime;

			// test à base de impulse 
			//GameEntity victimEntity = victim.AgentVisuals.GetEntity();
			//victimEntity.AddPhysics(60, Vec3.One, PhysicsShape.GetFromResource("bo_axe_short", false), Vec3.One, Vec3.One, PhysicsMaterial.GetFromName("adobe"), false, 1000);
			//if (victimEntity.HasPhysicsBody())
			//{
			//	victimEntity.ApplyLocalImpulseToDynamicBody(victim.GetChestGlobalPosition(), projectionVector * 100f);
			//}

			// List of all agents already hit by the projecting plane or the victim
			List<Agent> collidedAgents = new List<Agent> { victim };

			Vec3 normalizedProjVector = projectionVector.NormalizedCopy();
			GameEntity projectingPlane = GameEntity.Instantiate(Current.Scene, "script_hit_plane", false);
			projectingPlane.BodyFlag |= BodyFlags.Barrier;

			// Calculate plane original position, based on victim position and offsets defined
			Vec3 originalPosition = victim.Position + Vec3.Up * 0f/*planeHeightOffset*/ + normalizedProjVector * PLANE_VECTOR_OFFSET;
			// Calculate the rotation to align the carpet with the push vector
			MatrixFrame originalFrame = CalculateAlignedFrame(originalPosition, projectionVector);
			originalFrame.Scale(new Vec3(2f, 2f, 2f));

			// Set the frame (position and orientation) of the projecting plane
			projectingPlane.SetGlobalFrame(originalFrame);

			while (Current.CurrentTime - startTime < projectionDuration)
			{
				List<Agent> collidingAgents = Current.GetNearbyAgents(
					victim.Position.AsVec2,
					3f,
					new MBList<Agent>()
				).FindAll(agent => !collidedAgents.Contains(agent) && victim.Position.Z - agent.Position.Z < 2f);

				// Add agents hit to collided list to ignore them next time
				collidedAgents.AddRange(collidingAgents);

				// Do a blow to every colliding agents
				foreach (Agent agent in collidingAgents)
				{
					victim.DealDamage(agent, 5);
					//agent.DealDamage(10, victim.Position, victim, true, true);
				}

				float elapsedSeconds = Current.CurrentTime - startTime;
				float t = elapsedSeconds / projectionDuration;
				float lerpT = SmoothStep(0f, 1f, t);
				float currentDistance = projectionVector.Length * lerpT;

				// Calculate the new position without gravity
				Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

				// Apply gravity to the newPosition
				float gravityOffset = 0f;// 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
				newPosition += -Vec3.Up * gravityOffset;

				// Move the projecting plane entity to the new position
				projectingPlane.SetLocalPosition(newPosition + Vec3.Up * PLANE_HEIGHT_OFFSET);

				// Add a delay to refresh around 50 times per second
				await Task.Delay(20);
			}


			//victim.SetActionChannel(0, fallbackAction, true, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);

			Log("Projection ended after hitting " + collidedAgents.Count + " agents. Original pos : " + originalPosition + " | Final pos : " + projectingPlane.GlobalPosition, LogLevel.Debug);

			// Remove the plane entity
			projectingPlane.Remove(0);
		}

		/*public static async Task FlyCarpet6(Agent victim, Vec3 projectionVector, float duration)
        {
            int i = 0;
            
            //ActionIndexCache horseFallbackAction = ActionIndexCache.Create("act_horse_fall_backwards");
            //ActionIndexCache fallbackAction = ActionIndexCache.Create("act_strike_fall_back_back_rise");
            //ActionIndexCache fallbackLeftAction = ActionIndexCache.Create("act_strike_fall_back_back_rise_left_stance");
            //ActionIndexCache fallbackHAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise");
            //ActionIndexCache fallbackHLeftAction = ActionIndexCache.Create("act_strike_fall_back_heavy_back_rise_left_stance");

            float startTime = Mission.Current.CurrentTime;
            //Vec3 agentPosition = victim.Position;
            float distance = projectionVector.Length;

            // Calculate the gravity force
            Vec3 gravity = -Vec3.Up * gravityMagnitude;

            // Create the flying carpet entity
            GameEntity flyingCarpet = GameEntity.Instantiate(Mission.Current.Scene, "flying_carpet", false);
            //flyingCarpet.BodyFlag |= BodyFlags.Barrier3D; Prevent flying to the moon but less satisfying physic :(
            // Calculate the position of the plane in front of the agent
            Vec3 originalPosition = agentPosition + Vec3.Up * planeHeightOffset + projectionVector.NormalizedCopy() * planeVectorOffset;
            InformationManager.DisplayMessage(new InformationMessage("AgentPos = " + agentPosition, Colors.Green));
            InformationManager.DisplayMessage(new InformationMessage("carpetPos = " + originalPosition, Colors.Green));

            // Set the local position of the flying carpet entity
            //flyingCarpet.SetLocalPosition(originalPosition);

            // Calculate the rotation to align the carpet with the push vector
            MatrixFrame frame = CalculateFrame(originalPosition, projectionVector);

            // Set the frame (position and orientation) of the flying carpet entity
            flyingCarpet.SetGlobalFrame(frame);

            Vec3 normalizedProjVector = projectionVector.NormalizedCopy();
            List<Agent> collidedAgents = new List<Agent>();
            while (Mission.Current.CurrentTime - startTime < duration)
            {
                float elapsedTime = Mission.Current.CurrentTime - startTime;
                float percentageDuration = elapsedTime / duration;
                Vec2 projectedDirection = projectionVector.AsVec2;

                List<Agent> collidingAgents = Mission.Current.GetNearbyAgents(
                    flyingCarpet.GetGlobalFrame().origin.AsVec2,
                    1f,
                    new MBList<Agent>()
                ).FindAll(agent => !collidedAgents.Contains(agent) && flyingCarpet.GetGlobalFrame().origin.Z - agent.Position.Z < 2f);

                collidedAgents.AddRange(collidingAgents);

                foreach (Agent agent in collidingAgents)
                {
                    // Get the agent's forward direction
                    Vec3 agentForward = agent.GetMovementDirection().ToVec3();

                    // Calculate the vector from the agent to the flying carpet
                    Vec3 agentToCarpet = flyingCarpet.GetGlobalFrame().origin - agent.Position;

                    // Calculate the angle between the agent's forward direction and the vector to the carpet
                    float angle = Vec3.AngleBetweenTwoVectors(agentForward, agentToCarpet);

                    // Convert the angle to degrees
                    float angleDegrees = angle * (180f / MathF.PI);

                    // Calculate the cross product of the agent's forward direction and the vector to the carpet
                    Vec3 crossProduct = Vec3.CrossProduct(agentForward, agentToCarpet);

                    // Check if the flying carpet is coming from the right or left side of the agent
                    bool fromRight = crossProduct.y > 0f;

                    // Check the strength of the flying carpet
                    bool isStrong = percentageDuration < 0.50f;
                    Logger.Log(angle + " || " + angleDegrees, LogLevel.Information, ConsoleColor.Red);
                    if (percentageDuration > 0.75f)
                    {

                    }
                    else if (agent.HasMount)
                    {
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, horseFallbackAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (fromRight && !isStrong)
                    {
                        Logger.Log("Weak hit coming from right");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (!fromRight && !isStrong)
                    {
                        Logger.Log("Weak hit coming from left");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackLeftAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (fromRight && isStrong)
                    {
                        Logger.Log("Strong hit coming from right");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackHAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                    else if (!fromRight && isStrong)
                    {
                        Logger.Log("Strong hit coming from left");
                        agent.LookDirection = originalPosition;
                        agent.SetActionChannel(0, fallbackHLeftAction, false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.1f, 0, true);
                    }
                }

                float elapsedSeconds = Mission.Current.CurrentTime - startTime;
                float t = elapsedSeconds / duration;
                float lerpT = SmoothStep(0f, 1f, t);
                float currentDistance = distance * lerpT;

                // Calculate the new position without gravity
                Vec3 newPosition = originalPosition + normalizedProjVector * currentDistance;

                // Apply gravity to the newPosition
                float gravityOffset = 0.5f * gravityMagnitude * elapsedSeconds * elapsedSeconds;
                newPosition += -Vec3.Up * gravityOffset;

                // Move the flying carpet entity to the new position
                flyingCarpet.SetLocalPosition(newPosition + Vec3.Up * planeHeightOffset);

                i++;

                await Task.Yield();
            }

            InformationManager.DisplayMessage(new InformationMessage("Carpet updated " + i + " times. Original pos : " + originalPosition + " | Final pos : " + flyingCarpet.GlobalPosition, Colors.Green));

            // Remove the carpet entity
            flyingCarpet.Remove(0);
        }*/

		/// <summary>
		/// Returns a matrix frame with vectors aligned to the given push vector.
		/// </summary>
		private static MatrixFrame CalculateAlignedFrame(Vec3 position, Vec3 pushVector)
		{
			// We want the upward vector to be aligned on push vector
			Vec3 upVector = pushVector;
			upVector.Normalize();

			Vec3 forwardVector = Vec3.Forward;

			// Calculate the right vector perpendicular to the up vector and forward vector
			Vec3 rightVector = Vec3.CrossProduct(upVector, forwardVector);
			rightVector.Normalize();

			// Recalculate the forwardVector vector to ensure it is perpendicular to the up and right vectors
			forwardVector = Vec3.CrossProduct(rightVector, upVector);
			forwardVector.Normalize();

			MatrixFrame frame = new MatrixFrame();
			frame.origin = position;
			frame.rotation.u = upVector;
			frame.rotation.f = forwardVector;
			frame.rotation.s = rightVector;

			return frame;
		}

		private static float SmoothStep(float edge0, float edge1, float x)
		{
			float t = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
			return t * t * (3f - 2f * t);
		}

		public static async Task PerformProjection(Agent victim, Vec3 projectionVector, float duration)
		{
			float startTime = Current.CurrentTime;
			Vec3 originalPosition = victim.Position;
			//Vec3 targetPosition = originalPosition + projectionVector;
			float distance = projectionVector.Length;

			while (Current.CurrentTime - startTime < duration)
			{
				float t = (Current.CurrentTime - startTime) / duration;
				float lerpT = SmoothStep(0f, 1f, t);
				float currentDistance = distance * lerpT;
				Vec3 newPosition = originalPosition + projectionVector.NormalizedCopy() * currentDistance;

				victim.TeleportToPosition(newPosition);

				await Task.Yield();
			}
		}

		private static bool IsBackAttack(Agent attacker, Agent victim)
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

			//Logger.Log(position);
			//Logger.Log("angleInRadians: " + angleInRadians.ToString());
			//Logger.Log("angleInDegrees: " + angleInDegrees.ToString());
			return backAttack;
		}

		public static AttackCollisionData SetAttackCollisionData(AttackCollisionData data, bool attackBlockedWithShield, bool collidedWithShieldOnBack, bool missileBlockedWithWeapon, bool missileHasPhysics, int physicsMaterialIndex, bool isColliderAgent, CombatCollisionResult collisionResult)
		{
			return AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
				attackBlockedWithShield,
				data.CorrectSideShieldBlock,
				data.IsAlternativeAttack,
				isColliderAgent,
				collidedWithShieldOnBack,
				data.IsMissile,
				missileBlockedWithWeapon,
				missileHasPhysics,
				data.EntityExists,
				data.ThrustTipHit,
				data.MissileGoneUnderWater,
				data.MissileGoneOutOfBorder,
				collisionResult,
				data.AffectorWeaponSlotOrMissileIndex,
				data.StrikeType,
				data.DamageType,
				data.CollisionBoneIndex,
				data.VictimHitBodyPart,
				data.AttackBoneIndex,
				data.AttackDirection,
				physicsMaterialIndex,
				data.CollisionHitResultFlags,
				data.AttackProgress,
				data.CollisionDistanceOnWeapon,
				data.AttackerStunPeriod,
				data.DefenderStunPeriod,
				data.MissileTotalDamage,
				data.MissileStartingBaseSpeed,
				data.ChargeVelocity,
				data.FallSpeed,
				data.WeaponRotUp,
				data.WeaponBlowDir,
				data.CollisionGlobalPosition,
				data.MissileVelocity,
				data.MissileStartingPosition,
				data.VictimAgentCurVelocity,
				data.CollisionGlobalNormal
			);
		}
	}
}
