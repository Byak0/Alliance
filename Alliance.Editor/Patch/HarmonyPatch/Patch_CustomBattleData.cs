using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.CustomBattle.CustomBattle;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch CustomBattleData to add new commanders / factions.
	/// </summary>
	class Patch_CustomBattleData
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_CustomBattleData));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				// Patch the static property getter for `Characters`
				Harmony.Patch(
					typeof(CustomBattleData).GetProperty(nameof(CustomBattleData.Characters), BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
					prefix: new HarmonyMethod(typeof(Patch_CustomBattleData).GetMethod(
						nameof(Prefix_Characters), BindingFlags.Static | BindingFlags.Public)));

				// Patch the static property getter for `Factions`
				Harmony.Patch(
					typeof(CustomBattleData).GetProperty(nameof(CustomBattleData.Factions), BindingFlags.Static | BindingFlags.Public).GetGetMethod(),
					prefix: new HarmonyMethod(typeof(Patch_CustomBattleData).GetMethod(
						nameof(Prefix_Factions), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_CustomBattleData)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Prefix for the `Characters` getter method to return custom characters.
		/// </summary>
		public static bool Prefix_Characters(ref IEnumerable<BasicCharacterObject> __result)
		{
			List<BasicCharacterObject> customCharacters = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>().Where(bco => bco.IsHero || bco.StringId.ToLower().Contains("hero")).ToList();

			// Set the result to your custom characters
			__result = customCharacters;

			// Return false to skip the original method
			return false;
		}

		/// <summary>
		/// Prefix for the `Factions` getter method to return custom factions.
		/// </summary>
		public static bool Prefix_Factions(ref IEnumerable<BasicCultureObject> __result)
		{
			List<BasicCultureObject> customFactions = MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().Where(culture => culture.IsMainCulture).ToList();

			// Set the result to your custom factions
			__result = customFactions;

			// Return false to skip the original method
			return false;
		}
	}
}
