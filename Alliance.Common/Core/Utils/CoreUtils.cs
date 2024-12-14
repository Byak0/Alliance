using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Common.Core.Utils
{
	static class CoreUtils
	{
		public static void TakeDamage(Agent victim, int damage, float magnitude = 50f, bool knockDown = false)
		{
			TakeDamage(victim, victim, damage, magnitude, knockDown);
		}

		public static void TakeDamage(Agent victim, Agent attacker, int damage, float magnitude = 50f, bool knockDown = false)
		{
			if (victim == null || attacker == null)
			{
				Log("Victim and/or attacker is null. Damage skipped", LogLevel.Warning);
				return;
			};

			if (victim.Health <= 0) return;

			Blow blow = new Blow(attacker.Index);
			blow.DamageType = DamageTypes.Pierce;
			blow.BoneIndex = victim.Monster.HeadLookDirectionBoneIndex;
			// Calculate blow position as the center of victim and attacker
			blow.GlobalPosition = (attacker.Position + victim.Position) * 0.5f;
			blow.GlobalPosition.z = blow.GlobalPosition.z + victim.GetEyeGlobalHeight();
			blow.BaseMagnitude = magnitude;
			blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
			blow.InflictedDamage = damage;
			blow.SwingDirection = victim.LookDirection;
			MatrixFrame frame = victim.Frame;
			blow.SwingDirection = frame.rotation.TransformToParent(new Vec3(-1f, 0f, 0f, -1f));
			blow.SwingDirection.Normalize();
			blow.Direction = blow.SwingDirection;
			blow.DamageCalculated = true;
			if (knockDown)
			{
				if (victim.HasMount) blow.BlowFlag |= BlowFlags.CanDismount;
				else blow.BlowFlag |= BlowFlags.KnockDown;
			}
			sbyte mainHandItemBoneIndex = attacker.Monster.MainHandItemBoneIndex;
			AttackCollisionData attackCollisionDataForDebugPurpose = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
				false,
				false,
				false,
				true,
				false,
				false,
				false,
				false,
				false,
				false,
				false,
				false,
				CombatCollisionResult.StrikeAgent,
				-1,
				0,
				2,
				blow.BoneIndex,
				BoneBodyPartType.Head,
				mainHandItemBoneIndex,
				UsageDirection.AttackLeft,
				-1,
				CombatHitResultFlags.NormalHit,
				0.5f,
				1f,
				0f,
				0f,
				0f,
				0f,
				0f,
				0f,
				Vec3.Up,
				blow.Direction,
				blow.GlobalPosition,
				Vec3.Zero,
				Vec3.Zero,
				victim.Velocity,
				Vec3.Up
			);
			victim.RegisterBlow(blow, attackCollisionDataForDebugPurpose);
		}

		/// <summary>
		/// Return the list of all agents alives that are near the target.
		/// IT WILL NOT INCLUDE THE MOUNT OF THE TARGET IF THE TARGET IS MOUNTED
		/// </summary>
		public static List<Agent> GetNearAliveAgentsInRange(float range, Agent target)
		{
			//Contain all agent (Players/Bots/Ridings)
			List<Agent> allAgents = Mission.Current.AllAgents;
			List<Agent> agentsInRange = new List<Agent>();

			foreach (Agent agent in allAgents)
			{
				if (!agent.IsActive()) continue;

				// Do not include mount of player and player.
				if (agent == target.MountAgent || agent == target) continue;

				float distance = agent.Position.Distance(target.Position);

				//Add offset in case of mount since mount are large
				if (agent.IsMount)
				{
					distance -= 0.5f;
				}

				if (distance < range)
				{
					agentsInRange.Add(agent);
				}
			}

			return agentsInRange;
		}

		/// <summary>
		/// Return the list of all agents alives that are near the position.
		/// </summary>
		public static List<Agent> GetNearAliveAgentsInRange(float range, Vec3 target)
		{
			//Contain all agent (Players/Bots/Ridings)
			List<Agent> allAgents = Mission.Current.AllAgents;
			List<Agent> agentsInRange = new List<Agent>();

			foreach (Agent agent in allAgents)
			{
				if (!agent.IsActive()) continue;

				float distance = agent.Position.Distance(target);

				//Add offset in case of mount since mount are large
				if (agent.IsMount)
				{
					distance -= 0.5f;
				}

				if (distance < range)
				{
					agentsInRange.Add(agent);
				}
			}

			return agentsInRange;
		}

		/// <summary>
		/// Return the number of seconds since the mission started.
		/// </summary>
		public static float GetMissionTimeInSeconds(this Mission mission)
		{
			return mission.MissionTimeTracker.NumberOfTicks / 10000000f;
		}
	}
}
