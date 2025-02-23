using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch some classes to remove completely useless and annoying asserts.
	/// </summary>
	class Patch_ActionSetCode
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_ActionSetCode));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				// Patch GenerateActionSetNameWithSuffix method for more logical action sets
				Harmony.Patch(
					typeof(ActionSetCode).GetMethod(nameof(ActionSetCode.GenerateActionSetNameWithSuffix), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_ActionSetCode).GetMethod(
						nameof(Prefix_ActionSetCode_GenerateActionSetNameWithSuffix), BindingFlags.Static | BindingFlags.Public)));

			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_ActionSetCode)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Return more logical action set.
		/// IE don't return as_uruk_villager if uruk use as_human_warrior as base
		/// or as_olog_villager if olog use as_troll_warrior as base...
		/// </summary>
		public static bool Prefix_ActionSetCode_GenerateActionSetNameWithSuffix(Monster monster, bool isFemale, string suffix, ref string __result)
		{
			if (monster == null)
			{
				__result = "as_human" + (isFemale ? "_female" : "") + suffix;
			}
			else if (monster.ActionSetCode.EndsWith("_warrior"))
			{
				__result = monster.ActionSetCode.Replace("_warrior", "") + (isFemale ? "_female" : "") + suffix;
			}
			else
			{
				__result = "as_" + (string.IsNullOrEmpty(monster.BaseMonster) ? monster.StringId : monster.BaseMonster) + (isFemale ? "_female" : "") + suffix;
			}

			return false; // Skip original method
		}
	}
}