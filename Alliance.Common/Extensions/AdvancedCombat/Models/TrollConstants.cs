using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.AdvancedCombat.Models
{
	public static class TrollConstants
	{
		public static readonly List<Animation> IdleAnimations;
		public static readonly List<Animation> RageAnimations;
		public static readonly List<Animation> SearchAnimations;
		public static readonly Animation StompAnimation;
		public static readonly List<sbyte> CollisionBones = new List<sbyte>
			{
				19, // ID left hand bone
				26, // ID right hand bone
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
			Animation rage1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_taunt_rage_1"); // stuck
			Animation rage2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_taunt_rage_2");
			Animation rage3 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_taunt_rage_3"); // stuck

			//Animation idle1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_idle_1");
			//Animation idle2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_idle_2");

			Animation search1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_taunt_23");
			Animation search2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_taunt_24");


			IdleAnimations = new List<Animation>() { rage1, rage2, rage3 };
			RageAnimations = new List<Animation>() { rage1, rage2, rage3 };
			SearchAnimations = new List<Animation>() { search1, search2 };
			StompAnimation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_taunt_25");
		}
	}
}
