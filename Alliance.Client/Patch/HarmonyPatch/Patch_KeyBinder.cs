using Alliance.Client.Core.KeyBinder;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.Options;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_KeyBinder
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_KeyBinder));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched) return false;
                _patched = true;

                Harmony.Patch(
                    typeof(HotKeyManager).GetMethod(nameof(HotKeyManager.RegisterInitialContexts),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_KeyBinder).GetMethod(
                        nameof(PrefixRegisterInitialContexts), BindingFlags.Static | BindingFlags.Public)));

                Harmony.Patch(
                    typeof(OptionsProvider).GetMethod(nameof(OptionsProvider.GetGameKeyCategoriesList),
                        BindingFlags.Static | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_KeyBinder).GetMethod(
                        nameof(PostfixGetGameKeyCategoriesList), BindingFlags.Static | BindingFlags.Public)));

            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_KeyBinder)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Add our own GameKeyContext to the native list
        // This is required since 1.2 now clears all categories when registering contexts
        public static bool PrefixRegisterInitialContexts(ref IEnumerable<GameKeyContext> contexts)
        {
            List<GameKeyContext> newContexts = contexts.ToList();
            foreach (GameKeyContext context in KeyBinder.KeyContexts.Values)
            {
                newContexts.Add(context);
            }
            contexts = newContexts;
            return true;
        }

        // Add our own key categories to native menu
        public static IEnumerable<string> PostfixGetGameKeyCategoriesList(IEnumerable<string> __result)
        {
            IEnumerable<string> categories = KeyBinder.KeysCategories.Select(c => c.CategoryId).Distinct();
            foreach (string cat in categories)
            {
                __result = CollectionExtensions.AddItem(__result, cat);
            }
            return __result;
        }
    }
}
