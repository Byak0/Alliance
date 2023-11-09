using Alliance.Client.Core.KeyBinder;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade.Options;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_GameKeyOptionsCategory
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_GameKeyOptionsCategory));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;

                Harmony.Patch(
                    typeof(OptionsProvider).GetMethod(nameof(OptionsProvider.GetGameKeyCategoriesList),
                        BindingFlags.Static | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_GameKeyOptionsCategory).GetMethod(
                        nameof(Postfix), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_GameKeyOptionsCategory)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Patch for KeyBinder system to add custom bindings to native menu
        public static IEnumerable<string> Postfix(IEnumerable<string> __result)
        {
            var categories = KeyBinder.KeysCategories.Select(c => c.CategoryId).Distinct();
            foreach (var cat in categories)
            {
                __result = CollectionExtensions.AddItem(__result, cat);
            }
            return __result;
        }
    }
}
