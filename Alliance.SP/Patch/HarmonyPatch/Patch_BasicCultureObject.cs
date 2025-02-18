using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.SP.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch BasicCultureObject to allow custom cultures.
	/// </summary>
	class Patch_BasicCultureObject
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_BasicCultureObject));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				Harmony.Patch(
					typeof(BasicCultureObject).GetMethod(nameof(BasicCultureObject.GetCultureCode), BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_BasicCultureObject).GetMethod(
						nameof(Prefix_GetCultureCode), BindingFlags.Static | BindingFlags.Public)));

			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_BasicCultureObject)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Prefix for "GetCultureCode" to prevent errors when using custom factions.
		/// </summary>
		public static bool Prefix_GetCultureCode(ref CultureCode __result, BasicCultureObject __instance)
		{
			if (Enum.TryParse(__instance.StringId, true, out CultureCode cultureCode))
			{
				__result = cultureCode;
			}
			else
			{
				__result = CultureCode.AnyOtherCulture;
			}

			// Return false to skip the original method
			return false;
		}
	}
}
