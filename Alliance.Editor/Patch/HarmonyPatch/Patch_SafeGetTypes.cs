using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.Patch.HarmonyPatch
{
	/// <summary>
	/// Global safeguard: catches ReflectionTypeLoadException anywhere GetTypes() is used.
	/// This prevents crashes caused by WPF or external assemblies during Bannerlord reflection.
	/// </summary>
	class Patch_SafeGetTypes
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_SafeGetTypes));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				Harmony.ReversePatch(
					typeof(Assembly).GetMethod(
						nameof(Assembly.GetTypes), BindingFlags.Instance | BindingFlags.Public),
					new HarmonyMethod(typeof(Patch_SafeGetTypes).GetMethod(nameof(Original_GetTypes))));

				Harmony.Patch(
					typeof(Assembly).GetMethod(nameof(Assembly.GetTypes),
						BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_SafeGetTypes).GetMethod(
						nameof(Prefix_GetTypes), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_SafeGetTypes)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		public static Type[] Original_GetTypes(Assembly __instance)
		{
			// Harmony replaces this with the original method
			throw new NotImplementedException("I'm never executed :(");
		}

		// Replace Assembly.GetTypes() globally
		public static bool Prefix_GetTypes(Assembly __instance, ref Type[] __result)
		{
			try
			{
				// Call original in a try-catch
				__result = Original_GetTypes(__instance);
				return false; // skip original
			}
			catch (ReflectionTypeLoadException e)
			{
				Log($"[CCU] Ignored ReflectionTypeLoadException in {__instance.GetName().Name}: {e.LoaderExceptions?.FirstOrDefault()?.Message}", LogLevel.Debug);
				__result = e.Types.Where(t => t != null).ToArray();
				return false; // skip original
			}
			catch (Exception ex)
			{
				Log($"[CCU] SafeGetTypes error for {__instance.GetName().Name} -> {ex.Message}", LogLevel.Error);
				__result = Array.Empty<Type>();
				return false;
			}
		}
	}
}
