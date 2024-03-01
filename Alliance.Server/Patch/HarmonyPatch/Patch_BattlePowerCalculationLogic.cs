using Alliance.Server.Extensions.AIBehavior;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_BattlePowerCalculationLogic
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_BattlePowerCalculationLogic));
        private static readonly ALBattlePowerCalculationLogic battlePowerLogic = new ALBattlePowerCalculationLogic();

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(BattlePowerCalculationLogic).GetMethod(nameof(BattlePowerCalculationLogic.GetTotalTeamPower),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_BattlePowerCalculationLogic).GetMethod(
                        nameof(Prefix_GetTotalTeamPower), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_BattlePowerCalculationLogic)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Hijack native BattlePowerCalculationLogic and make it use our ALBattlePowerCalculationLogic instead
        // as it it causing a crash by not using the correct number of teams.
        public static bool Prefix_GetTotalTeamPower(Team team, ref float __result)
        {
            __result = battlePowerLogic.GetTotalTeamPower(team);

            // Skip original method
            return false;
        }
    }
}
