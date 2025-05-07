using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.Models
{
	public static class TrollConstants
	{
		public static readonly List<ActionIndexCache> IdleAnimations;
		public static readonly List<ActionIndexCache> RageAnimations;
		public static readonly List<ActionIndexCache> SearchAnimations;
		public static readonly ActionIndexCache StompAnimation;
		public static readonly List<sbyte> HandCollisionBones = new List<sbyte>
			{
				19, // ID left hand bone
				26, // ID right hand bone
			};

		public static readonly List<sbyte> FootCollisionBones = new List<sbyte>
			{
				3, // ID left foot bone
				7, // ID right foot bone
			};

		public static readonly List<sbyte> CollisionBones = new List<sbyte>
			{
				19, // ID left hand bone
				26, // ID right hand bone
				3, // ID left foot bone
				7, // ID right foot bone
			};

		public const int CHASE_RADIUS = 30; // Max range to consider when chasing a target
		public const int THREAT_RANGE = 15; // Max range to consider when looking for threats
		public const float AI_REFRESH_TIME = 2f; // Default refresh time
		public const float CLOSE_RANGE_AI_REFRESH_TIME = 0.25f; // Refresh time when at close range, for better precision
		public const float LOW_HEALTH_THRESHOLD = 0.3f; // Threshold to consider as wounded
		public const float TARGET_LOSE_INTEREST_TIME = 30; // Delay before lose interest in target
		public const float KICK_COOLDOWN = 6; // Cooldown between attacks

		static TrollConstants()
		{
			ActionIndexCache rage1 = ActionIndexCache.Create("act_taunt_rage_1"); // stuck
			ActionIndexCache rage2 = ActionIndexCache.Create("act_taunt_rage_2");
			ActionIndexCache rage3 = ActionIndexCache.Create("act_taunt_rage_3"); // stuck

			//Animation idle1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_idle_1");
			//Animation idle2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_idle_2");

			ActionIndexCache search1 = ActionIndexCache.Create("act_taunt_23");
			ActionIndexCache search2 = ActionIndexCache.Create("act_taunt_24");


			IdleAnimations = new List<ActionIndexCache>() { rage1, rage2, rage3 };
			RageAnimations = new List<ActionIndexCache>() { rage1, rage2, rage3 };
			SearchAnimations = new List<ActionIndexCache>() { search1, search2 };
			StompAnimation = ActionIndexCache.Create("act_troll_stomp");
		}
	}
}
