using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Common.Core.Utils
{
	public delegate void RegisterBlowDelegate(Mission mission, Agent attacker, Agent victim, GameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon, ref CombatLogData combatLogData);
	public delegate Agent GetClosestEnemyAgentDelegate(Mission mission, MBTeam team, Vec3 position, float radius);
	public delegate Agent GetClosestAllyAgentDelegate(Mission mission, MBTeam team, Vec3 position, float radius);
	public delegate int GetNearbyEnemyAgentCountDelegate(Mission mission, MBTeam team, Vec2 position, float radius);

	public static class CoreUtils
	{
		private static readonly RegisterBlowDelegate _registerBlow;
		private static readonly GetClosestEnemyAgentDelegate _getClosestEnemyAgent;
		private static readonly GetClosestAllyAgentDelegate _getClosestAllyAgent;
		private static readonly GetNearbyEnemyAgentCountDelegate _getNearbyEnemyAgentCount;

		static CoreUtils()
		{
			// Cache RegisterBlow.
			MethodInfo blowMethod = typeof(Mission).GetMethod("RegisterBlow", BindingFlags.Instance | BindingFlags.NonPublic);
			_registerBlow = (RegisterBlowDelegate)Delegate.CreateDelegate(typeof(RegisterBlowDelegate), blowMethod);

			// Cache GetClosestEnemyAgent.
			MethodInfo enemyMethod = typeof(Mission).GetMethod("GetClosestEnemyAgent", BindingFlags.Instance | BindingFlags.NonPublic);
			_getClosestEnemyAgent = (GetClosestEnemyAgentDelegate)Delegate.CreateDelegate(typeof(GetClosestEnemyAgentDelegate), enemyMethod);

			// Cache GetClosestAllyAgent.
			MethodInfo allyMethod = typeof(Mission).GetMethod("GetClosestAllyAgent", BindingFlags.Instance | BindingFlags.NonPublic);
			_getClosestAllyAgent = (GetClosestAllyAgentDelegate)Delegate.CreateDelegate(typeof(GetClosestAllyAgentDelegate), allyMethod);

			// Cache GetNearbyEnemyAgentCount.
			MethodInfo countMethod = typeof(Mission).GetMethod("GetNearbyEnemyAgentCount", BindingFlags.Instance | BindingFlags.NonPublic);
			_getNearbyEnemyAgentCount = (GetNearbyEnemyAgentCountDelegate)Delegate.CreateDelegate(typeof(GetNearbyEnemyAgentCountDelegate), countMethod);
		}

		/// <summary>
		/// Wrapper to access private Mission.RegisterBlow.
		/// </summary>
		public static void RegisterBlow(Agent attacker, Agent victim, GameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon, ref CombatLogData combatLogData)
		{
			_registerBlow(Mission.Current, attacker, victim, realHitEntity, b, ref collisionData, attackerWeapon, ref combatLogData);
		}

		/// <summary>
		/// Wrapper to access private Mission.GetClosestEnemyAgent.
		/// </summary>
		public static Agent GetClosestEnemyAgent(MBTeam team, Vec3 position, float radius)
		{
			return _getClosestEnemyAgent(Mission.Current, team, position, radius);
		}

		/// <summary>
		/// Wrapper to access private Mission.GetClosestAllyAgent.
		/// </summary>
		public static Agent GetClosestAllyAgent(MBTeam team, Vec3 position, float radius)
		{
			return _getClosestAllyAgent(Mission.Current, team, position, radius);
		}

		/// <summary>
		/// Wrapper to access private Mission.GetNearbyEnemyAgentCount.
		/// </summary>
		public static int GetNearbyEnemyAgentCount(MBTeam team, Vec2 position, float radius)
		{
			return _getNearbyEnemyAgentCount(Mission.Current, team, position, radius);
		}

		/// <summary>
		/// Deal damage to an agent (self-inflicted).
		/// </summary>
		public static void TakeDamage(Agent victim, int damage, float magnitude = 50f, bool knockDown = false)
		{
			TakeDamage(victim, victim, damage, magnitude, knockDown);
		}

		/// <summary>
		/// Deal damage to an agent.
		/// </summary>
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
				BoneBodyPartType.Abdomen,
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

			CombatLogData combatLogData = new CombatLogData(false, attacker.IsHuman, attacker.IsMine, attacker.RiderAgent != null, attacker.RiderAgent != null && attacker.RiderAgent.IsMine, attacker.IsMount, victim.IsHuman, victim.IsMine, victim.Health <= 0f, victim.HasMount, victim.RiderAgent != null && victim.RiderAgent.IsMine, victim.IsMount, false, victim.RiderAgent == victim, knockDown, false, 0f);
			RegisterBlow(attacker, victim, null, blow, ref attackCollisionDataForDebugPurpose, MissionWeapon.Invalid, ref combatLogData);
		}

		// Reusable candidate list for spatial queries.
		private static readonly List<Agent> ReuseCandidateList = new List<Agent>(256);

		/// <summary>
		/// Returns a list of nearby alive agents. IT WILL NOT INCLUDE THE MOUNT OF THE TARGET IF THE TARGET IS MOUNTED.
		/// Using a simple spatial partitioning scheme and memory reuse.
		/// Assumes that SpatialGrid.UpdateGrid(agents) is called once per tick.
		/// </summary>
		public static List<Agent> GetNearAliveAgentsInRange(float range, Agent target)
		{
			// Query a simple spatial grid instead of iterating over all agents.
			Vec3 targetPos = target.Position;
			SpatialGrid.GetAgentsInRadius(targetPos, range, ReuseCandidateList);

			// Cache target values.
			Agent targetMount = target.MountAgent;

			// Precompute squared thresholds.
			float rangeSquared = range * range;
			float mountThresholdSquared = (range + 0.5f) * (range + 0.5f);

			int candidateCount = ReuseCandidateList.Count;
			List<Agent> result = new List<Agent>(candidateCount);

			// Allocate temporary arrays for candidate positions.
			float[] xs = new float[candidateCount];
			float[] ys = new float[candidateCount];
			float[] zs = new float[candidateCount];

			// Fill the arrays with positions.
			for (int i = 0; i < candidateCount; i++)
			{
				Vec3 pos = ReuseCandidateList[i].Position;
				xs[i] = pos.x;
				ys[i] = pos.y;
				zs[i] = pos.z;
			}

			// Process candidates.
			for (int i = 0; i < candidateCount; i++)
			{
				Agent agent = ReuseCandidateList[i];
				if (!agent.IsActive() || agent == target || agent == targetMount)
					continue;

				float dx = xs[i] - targetPos.x;
				float dy = ys[i] - targetPos.y;
				float dz = zs[i] - targetPos.z;
				float distanceSquared = dx * dx + dy * dy + dz * dz;

				float threshold = agent.IsMount ? mountThresholdSquared : rangeSquared;
				if (distanceSquared < threshold)
				{
					result.Add(agent);
				}
			}

			return result;
		}

		/// <summary>
		/// Return the list of all agents alives that are near the position.
		/// </summary>
		public static List<Agent> GetNearAliveAgentsInRange(float range, Vec3 targetPos)
		{
			// Query a simple spatial grid instead of iterating over all agents.
			SpatialGrid.GetAgentsInRadius(targetPos, range, ReuseCandidateList);

			// Precompute squared thresholds.
			float rangeSquared = range * range;
			float mountThresholdSquared = (range + 0.5f) * (range + 0.5f);

			int candidateCount = ReuseCandidateList.Count;
			List<Agent> result = new List<Agent>(candidateCount);

			// Allocate temporary arrays for candidate positions.
			float[] xs = new float[candidateCount];
			float[] ys = new float[candidateCount];
			float[] zs = new float[candidateCount];

			// Fill the arrays with positions.
			for (int i = 0; i < candidateCount; i++)
			{
				Vec3 pos = ReuseCandidateList[i].Position;
				xs[i] = pos.x;
				ys[i] = pos.y;
				zs[i] = pos.z;
			}

			// Process candidates.
			for (int i = 0; i < candidateCount; i++)
			{
				Agent agent = ReuseCandidateList[i];
				if (!agent.IsActive())
					continue;

				float dx = xs[i] - targetPos.x;
				float dy = ys[i] - targetPos.y;
				float dz = zs[i] - targetPos.z;
				float distanceSquared = dx * dx + dy * dy + dz * dz;

				float threshold = agent.IsMount ? mountThresholdSquared : rangeSquared;
				if (distanceSquared < threshold)
				{
					result.Add(agent);
				}
			}

			return result;
		}

		/// <summary>
		/// Return the number of seconds since the mission started.
		/// </summary>
		public static float GetMissionTimeInSeconds(this Mission mission)
		{
			return mission.MissionTimeTracker.NumberOfTicks / 10000000f;
		}

		/// <summary>
		/// Return the current number of players in the game (including bots from server settings).
		/// </summary>
		public static int CurrentPlayerCount
		{
			get
			{
				return GameNetwork.NetworkPeers.Count + MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue() + MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
			}
		}

		public static string FixUtf8Encoding(string text)
		{
			byte[] bytes = Encoding.Default.GetBytes(text); // interpret the string as Windows-1252
			return Encoding.UTF8.GetString(bytes); // reinterpret it as UTF-8
		}

		/// <summary>
		/// Returns a random position within the given center and radius.
		/// </summary>
		public static Vec3 GetRandomPositionWithinRadius(Vec3 center, float radius, bool validNavmeshOnly = false)
		{
			if (validNavmeshOnly) return Mission.Current.GetRandomPositionAroundPoint(center, 0, radius);

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

	public static class SpatialGrid
	{
		// A simple grid mapping cell coordinates to lists of agents.
		// For simplicity, we use a dictionary keyed by a tuple of ints.
		private static readonly Dictionary<(int, int, int), List<Agent>> Grid = new Dictionary<(int, int, int), List<Agent>>();
		public static float CellSize = 20f; // Tune this value as needed.

		// Call this once per tick to rebuild the grid.
		public static void UpdateGrid(List<Agent> agents)
		{
			Grid.Clear();
			foreach (Agent agent in agents)
			{
				if (!agent.IsActive())
					continue;
				var cell = GetCell(agent.Position);
				if (!Grid.TryGetValue(cell, out List<Agent> list))
				{
					list = new List<Agent>();
					Grid[cell] = list;
				}
				list.Add(agent);
			}
		}

		private static (int, int, int) GetCell(Vec3 pos)
		{
			return (
				(int)Math.Floor(pos.x / CellSize),
				(int)Math.Floor(pos.y / CellSize),
				(int)Math.Floor(pos.z / CellSize)
			);
		}

		/// <summary>
		/// Fills the provided list with agents whose positions fall within the given radius of center.
		/// The list is cleared before adding.
		/// </summary>
		public static void GetAgentsInRadius(Vec3 center, float radius, List<Agent> reuseList)
		{
			reuseList.Clear();
			float radiusSquared = radius * radius;
			int minX = (int)Math.Floor((center.x - radius) / CellSize);
			int maxX = (int)Math.Floor((center.x + radius) / CellSize);
			int minY = (int)Math.Floor((center.y - radius) / CellSize);
			int maxY = (int)Math.Floor((center.y + radius) / CellSize);
			int minZ = (int)Math.Floor((center.z - radius) / CellSize);
			int maxZ = (int)Math.Floor((center.z + radius) / CellSize);

			// Iterate over grid cells.
			foreach (var kvp in Grid)
			{
				var key = kvp.Key;
				// Check if the cell is within the bounding box.
				if (key.Item1 >= minX && key.Item1 <= maxX &&
					key.Item2 >= minY && key.Item2 <= maxY &&
					key.Item3 >= minZ && key.Item3 <= maxZ)
				{
					foreach (Agent agent in kvp.Value)
					{
						// Quick check: if within the radius.
						float dx = agent.Position.x - center.x;
						float dy = agent.Position.y - center.y;
						float dz = agent.Position.z - center.z;
						if (dx * dx + dy * dy + dz * dz <= radiusSquared)
						{
							reuseList.Add(agent);
						}
					}
				}
			}
		}
	}
}