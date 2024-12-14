using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.AdvancedCombat.Models
{
	public static class AdvancedCombatConstants
	{
		public static readonly List<Animation> FrontProjectionAnimations;
		public static readonly List<Animation> BackProjectionAnimations;
		public static readonly List<Animation> LeftProjectionAnimations;
		public static readonly List<Animation> RightProjectionAnimations;

		static AdvancedCombatConstants()
		{
			Animation backProjection1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_knock_back_abdomen_front"); // +
			Animation backProjection2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_back_back_rise"); // +
			Animation backProjection3 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_back_back_rise_left_stance"); // +
			Animation backProjection4 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_back_heavy_back_rise"); // +++

			Animation frontProjection1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_knock_back_abdomen_back"); // +
			Animation frontProjection2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_front_back_rise"); // ++
			Animation frontProjection3 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_front_back_rise_left_stance"); // ++
			Animation frontProjection4 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_front_heavy_back_rise"); // +++

			Animation leftProjection1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_knock_back_abdomen_left"); // +
			Animation leftProjection2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_left_back_rise_alt"); // +
			Animation leftProjection3 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_left_back_rise_left_stance_alt"); // +
			Animation leftProjection4 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_left_back_rise"); // ++
			Animation leftProjection5 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_left_back_rise_left_stance"); // ++
			Animation leftProjection6 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_left_heavy_back_rise"); // +++
			Animation leftProjection7 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_left_heavy_back_rise_left_stance"); // +++

			Animation rightProjection1 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_knock_back_abdomen_right"); // +
			Animation rightProjection2 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_right_back_rise_alt"); // +
			Animation rightProjection3 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_right_back_rise_left_stance_alt"); // +
			Animation rightProjection4 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_right_back_rise"); // ++
			Animation rightProjection5 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_right_back_rise_left_stance"); // ++
			Animation rightProjection6 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_right_heavy_back_rise"); // +++
			Animation rightProjection7 = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_strike_fall_right_heavy_back_rise_left_stance"); // +++

			FrontProjectionAnimations = new List<Animation>() { frontProjection1, frontProjection2, frontProjection3, frontProjection4 };
			BackProjectionAnimations = new List<Animation>() { backProjection1, backProjection2, backProjection3, backProjection4 };
			LeftProjectionAnimations = new List<Animation>() { leftProjection1, leftProjection2, leftProjection3, leftProjection4, leftProjection5, leftProjection6, leftProjection7 };
			RightProjectionAnimations = new List<Animation>() { rightProjection1, rightProjection2, rightProjection3, rightProjection4, rightProjection5, rightProjection6, rightProjection7 };
		}
	}
}
