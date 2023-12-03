using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Multiplayer.Admin;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_DefaultAdminPanelOptionProvider
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_DefaultAdminPanelOptionProvider));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(DefaultAdminPanelOptionProvider).GetMethod(nameof(DefaultAdminPanelOptionProvider.GetOptionGroups),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_DefaultAdminPanelOptionProvider).GetMethod(
                        nameof(Prefix_GetOptionGroups), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_DefaultAdminPanelOptionProvider)}", LogLevel.Error);
                Log(e.Message, LogLevel.Error);
                return false;
            }

            return true;
        }

        // Load GetMissionOptions in every case to prevent native admin panel from crashing
        public static bool Prefix_GetOptionGroups(DefaultAdminPanelOptionProvider __instance, ref MBReadOnlyList<IAdminPanelOptionGroup> __result, ref MBList<IAdminPanelOptionGroup> ____optionGroups)
        {
            // Manually replicate the method's logic, excluding the if condition
            ____optionGroups.Clear();
            MethodInfo getMissionOptions = __instance.GetType().GetMethod("GetMissionOptions", BindingFlags.NonPublic | BindingFlags.Instance);
            ____optionGroups.Add((IAdminPanelOptionGroup)getMissionOptions.Invoke(__instance, null));
            MethodInfo getImmediateEffectOptions = __instance.GetType().GetMethod("GetImmediateEffectOptions", BindingFlags.NonPublic | BindingFlags.Instance);
            ____optionGroups.Add((IAdminPanelOptionGroup)getImmediateEffectOptions.Invoke(__instance, null));
            MethodInfo getActions = __instance.GetType().GetMethod("GetActions", BindingFlags.NonPublic | BindingFlags.Instance);
            ____optionGroups.Add((IAdminPanelOptionGroup)getActions.Invoke(__instance, null));

            __result = ____optionGroups;

            return false; // Skip the original method
        }

        /* Original method
        public MBReadOnlyList<IAdminPanelOptionGroup> GetOptionGroups()
		{
			this._optionGroups.Clear();
			if (MultiplayerIntermissionVotingManager.Instance.IsAutomatedBattleSwitchingEnabled)
			{
				this._optionGroups.Add(this.GetMissionOptions());
			}
			this._optionGroups.Add(this.GetImmediateEffectOptions());
			this._optionGroups.Add(this.GetActions());
			return this._optionGroups;
		}*/
    }
}
