using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.AdvancedCombat.Models
{
	public static class WargConstants
	{
		public static readonly List<sbyte> WargBoneIds = new List<sbyte>
			{
				22, // ID head bone
				23, // ID head bone
				36, // ID right paw
				37, // ID right paw
				42, // ID left paw
				43, // ID left paw
			};

		public static readonly List<Animation> IdleAnimations;
		public static readonly List<Animation> CarefulAnimations;
		public static readonly Animation AttackRunningAnimation;
		public static readonly Animation AttackAnimation;
		public static readonly Animation WalkBackAnimation;
		public static readonly Animation DefendAnimation;

		public const int CHASE_RADIUS = 40; // Max range to consider when chasing a target
		public const int THREAT_RANGE = 15; // Max range to consider when looking for threats
		public const float AI_REFRESH_TIME = 2f; // Default refresh time
		public const float CLOSE_RANGE_AI_REFRESH_TIME = 0.25f; // Refresh time when at close range, for better precision
		public const float LOW_HEALTH_THRESHOLD = 0.3f; // Threshold to consider warg as wounded
		public const float MAINTAIN_DISTANCE = 5f; // Distance to keep when being careful of target
		public const float TARGET_LOSE_INTEREST_TIME = 30; // Delay before warg lose interest in target
		public const float ATTACK_COOLDOWN = 4; // Cooldown between attacks

		static WargConstants()
		{
			Animation rear = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_rear");
			Animation idle1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_idle_1");
			Animation idle2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_idle_2");

			IdleAnimations = new List<Animation>() { rear, idle1, idle1, idle2, idle2 };
			CarefulAnimations = new List<Animation>() { rear, idle1, idle1, idle1, idle1, idle1 };
			AttackRunningAnimation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_attack_running");
			AttackAnimation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_attack_stand");
			WalkBackAnimation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_backward_walk_stand");
			DefendAnimation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_quick_release_thrust_2h");
		}
	}
}
