using Alliance.Common.Extensions.TroopSpawner.Models;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_Formation
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_Formation));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(Formation).GetMethod(nameof(Formation.SetControlledByAI),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_Formation).GetMethod(
                        nameof(Prefix_SetControlledByAI), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_Formation)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Hijack native SetControlledByAI and change parameter "enforceNotSplittableByAI" to true 
        /// if formation is player-controlled (to prevent split by AI).
        /// </summary>
        public static bool Prefix_SetControlledByAI(Formation __instance, bool isControlledByAI, ref bool enforceNotSplittableByAI)
        {
            if (FormationControlModel.Instance.GetControllerOfFormation(__instance) != null)
            {
                enforceNotSplittableByAI = true;
            }

            // Run original method
            return true;
        }
    }
}
