using Alliance.Common.Patch;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_Mission
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_Mission));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                var originalMethod = typeof(Mission).GetMethod("GetFreeRuntimeMissionObjectId",
                    BindingFlags.Instance | BindingFlags.Public);

                if (originalMethod == null)
                {
                    // Handle the case where the method is not found
                    return false;
                }

                Harmony.Patch(originalMethod, prefix: new HarmonyMethod(typeof(Patch_Mission).GetMethod(
                    nameof(GetFreeRuntimeMissionObjectIdPatch), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_Mission)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Increase max number of MissionObjects in a mission from 4095 to 10000.
        /// Credits to PersistentBannerlord@2023.
        /// </summary>
        public static bool GetFreeRuntimeMissionObjectIdPatch(Mission __instance, ref int __result, Stack<(int, float)> ____emptyRuntimeMissionObjectIds, ref int ____lastRuntimeMissionObjectIdCount)
        {
            float totalMissionTime = MBCommon.GetTotalMissionTime();
            int result = -1;

            if (____emptyRuntimeMissionObjectIds.Count > 0)
            {
                if (totalMissionTime - ____emptyRuntimeMissionObjectIds.Peek().Item2 > 30f || ____lastRuntimeMissionObjectIdCount >= DirtyCommonPatcher.MAX_MISSION_OBJECTS)
                {
                    result = ____emptyRuntimeMissionObjectIds.Pop().Item1;
                }
                else
                {
                    result = ____lastRuntimeMissionObjectIdCount;
                    ____lastRuntimeMissionObjectIdCount++;
                }
            }
            else if (____lastRuntimeMissionObjectIdCount < DirtyCommonPatcher.MAX_MISSION_OBJECTS)
            {
                result = ____lastRuntimeMissionObjectIdCount;
                ____lastRuntimeMissionObjectIdCount++;
            }

            __result = result;

            return false;
        }
    }
}
