﻿using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Agent;


namespace Alliance.Server.Core.Utils
{
	class CoreUtils
	{
		public static void TakeDamage(Agent victim, int damage, float magnitude = 50f)
		{
			TakeDamage(victim, victim, damage, magnitude);
		}

		/// <summary>
		/// Apply damage without weapon. eg : Warg Attack
		/// </summary>
		/// <param name="victim">Agent taking damage</param>
		/// <param name="attacker">Agent giving damage</param>
		/// /// <param name="damage">Amount of damage</param>
		/// <returns></returns>
		public static void TakeDamage(Agent victim, Agent attacker, int damage, float magnitude = 50f)
		{
			if (victim == null || attacker == null)
			{
				Common.Utilities.Logger.Log("Victim and/or attacker is null. Damage skipped", Common.Utilities.Logger.LogLevel.Warning);
				return;
			};

			if (victim.Health <= 0 || victim.IsFadingOut()) return;

			// Retrieve peer from either agent or rider agent
			MissionPeer peer = attacker?.MissionPeer != null ? attacker.MissionPeer : attacker.RiderAgent?.MissionPeer != null ? attacker.RiderAgent.MissionPeer : null;
			if (peer != null)
			{
				//Communicate damage to client to display PersonalKillFeed
				GameNetwork.BeginModuleEventAsServer(peer.GetNetworkPeer());
				GameNetwork.WriteMessage(new SyncPersonalKillFeedNotification(attacker, victim, false, false, false, victim.Health <= damage, false, false, damage));
				GameNetwork.EndModuleEventAsServer();
			}

			Blow blow = new Blow(attacker.Index);
			blow.DamageType = DamageTypes.Pierce;
			blow.BoneIndex = victim.Monster.HeadLookDirectionBoneIndex;
			blow.GlobalPosition = victim.GetChestGlobalPosition();
			blow.BaseMagnitude = magnitude;
			blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
			blow.InflictedDamage = damage;
			blow.SwingDirection = victim.LookDirection;
			MatrixFrame frame = victim.Frame;
			blow.SwingDirection = frame.rotation.TransformToParent(new Vec3(-1f, 0f, 0f, -1f));
			blow.SwingDirection.Normalize();
			blow.Direction = blow.SwingDirection;
			blow.DamageCalculated = true;
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
		/// Return the list of all agents alive that are near the target.
		/// IT WILL NOT INCLUDE THE MOUNT OF THE TARGET IF THE TARGET IS MOUNTED
		/// </summary>
		/// <param name="range"></param>
		/// <param name="target"></param>
		/// <returns></returns>
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
		/// Returns a random position within the given center and radius.
		/// </summary>
		public static Vec3 GetRandomPositionWithinRadius(Vec3 center, float radius)
		{
			// Generate random angle and distance within the radius
			float angle = MBRandom.RandomFloat * MathF.PI * 2; // Random angle between 0 and 360 degrees
			float distance = MBRandom.RandomFloat * radius; // Random distance within the radius

			// Calculate X and Y coordinates
			float x = center.x + MathF.Cos(angle) * distance;
			float y = center.y + MathF.Sin(angle) * distance;

			// Return the new position (Z remains unchanged)
			return new Vec3(x, y, center.z);
		}
	}
}
