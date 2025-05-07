using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.Models
{
	public static class WargConstants
	{
		public static readonly List<sbyte> WargBoneIds = new List<sbyte>
			{
				23, // ID head bone
				37, // ID right paw
				43, // ID left paw
			};

		public static readonly List<ActionIndexCache> IdleAnimations;
		public static readonly List<ActionIndexCache> CarefulAnimations;
		public static readonly ActionIndexCache AttackRunningAnimation;
		public static readonly ActionIndexCache AttackAnimation;
		public static readonly ActionIndexCache WalkBackAnimation;
		public static readonly ActionIndexCache RearAnimation;

		public const int CHASE_RADIUS = 40; // Max range to consider when chasing a target
		public const int THREAT_RANGE = 15; // Max range to consider when looking for threats
		public const float AI_REFRESH_TIME = 2f; // Default refresh time
		public const float CLOSE_RANGE_AI_REFRESH_TIME = 0.25f; // Refresh time when at close range, for better precision
		public const float LOW_HEALTH_THRESHOLD = 0.3f; // Threshold to consider warg as wounded
		public const float MAINTAIN_DISTANCE = 5f; // Distance to keep when being careful of target
		public const float TARGET_LOSE_INTEREST_TIME = 30; // Delay before warg lose interest in target
		public const float ATTACK_COOLDOWN = 8; // Cooldown between attacks

		static WargConstants()
		{
			ActionIndexCache idle1 = ActionIndexCache.Create("act_warg_idle_1");
			ActionIndexCache idle2 = ActionIndexCache.Create("act_warg_idle_2");

			RearAnimation = ActionIndexCache.Create("act_warg_rear");
			IdleAnimations = new List<ActionIndexCache>() { RearAnimation, idle1, idle1, idle2, idle2 };
			CarefulAnimations = new List<ActionIndexCache>() { RearAnimation, idle1, idle1, idle1, idle1, idle1 };
			AttackRunningAnimation = ActionIndexCache.Create("act_warg_attack_running");
			AttackAnimation = ActionIndexCache.Create("act_warg_attack_stand");
			WalkBackAnimation = ActionIndexCache.Create("act_warg_backward_walk_stand");
		}
	}
}
