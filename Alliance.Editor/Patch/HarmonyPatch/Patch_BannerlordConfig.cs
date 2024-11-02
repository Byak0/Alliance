using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch BannerlordConfig to change army sizes.
	/// </summary>
	class Patch_BannerlordConfig
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_BannerlordConfig));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				// Patch the static property getter for `MinBattleSize`
				Harmony.Patch(
					typeof(BannerlordConfig).GetProperty(nameof(BannerlordConfig.MinBattleSize), BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
					prefix: new HarmonyMethod(typeof(Patch_BannerlordConfig).GetMethod(
						nameof(Prefix_MinBattleSize), BindingFlags.Static | BindingFlags.Public)));

				// Patch the static property getter for `MaxBattleSize`
				Harmony.Patch(
					typeof(BannerlordConfig).GetProperty(nameof(BannerlordConfig.MaxBattleSize), BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
					prefix: new HarmonyMethod(typeof(Patch_BannerlordConfig).GetMethod(
						nameof(Prefix_MaxBattleSize), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_BannerlordConfig)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Prefix for the `MinBattleSize` getter method to return a custom minimum battle size.
		/// </summary>
		public static bool Prefix_MinBattleSize(ref int __result)
		{
			// Set a custom minimum battle size
			__result = 0;
			return false;     // Skip the original method
		}

		/// <summary>
		/// Prefix for the `MaxBattleSize` getter method to return a custom maximum battle size.
		/// </summary>
		public static bool Prefix_MaxBattleSize(ref int __result)
		{
			// Set a custom maximum battle size
			__result = 10000;
			return false;     // Skip the original method
		}
	}
}