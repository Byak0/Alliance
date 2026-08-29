#if !SERVER
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.MountAndBlade.View.Screens;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    /// <summary>
    /// Natively <c>MissionScreen.HandleUserInput</c> freezes input whenever <c>CustomCamera != null</c>.
    /// The transpiler swaps the native "CustomCamera == null" test
    /// for a call to <see cref="HandleInputNormally"/>: identical to native behavior unless
    /// <see cref="FreeInputEnabled"/> is set, in which case input stays live while a cinematic camera
    /// renders (cinematic <c>AgentBehaviorMode.Free</c>).
    /// Patch needed because <c>HandleUserInput</c> is not virtual - no clean override exists.
    /// </summary>
    class Patch_MissionScreen
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionScreen));
        private static bool _patched;

        /// <summary>Set by CinematicView while a Free-agent-mode cinematic is playing (input must stay live).</summary>
        public static bool FreeInputEnabled;

        private static readonly MethodInfo GateMethod = AccessTools.Method(typeof(Patch_MissionScreen), nameof(HandleInputNormally));

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                MethodInfo original = AccessTools.Method(typeof(MissionScreen), "HandleUserInput");
                if (original == null)
                {
                    Log($"{nameof(Patch_MissionScreen)}: MissionScreen.HandleUserInput not found", LogLevel.Error);
                    return false;
                }

                Harmony.Patch(original,
                    transpiler: new HarmonyMethod(AccessTools.Method(typeof(Patch_MissionScreen), nameof(Transpiler))));
                _patched = true;
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionScreen)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Runtime gate injected in place of the native "CustomCamera == null" test. When a Free-mode
        /// cinematic plays, input is handled as if no custom camera existed; otherwise native behavior.
        /// </summary>
        private static bool HandleInputNormally(MissionScreen screen)
        {
            return FreeInputEnabled || screen.CustomCamera == null;
        }

        /// <summary>
        /// Rewrites the IL sequence "call get_CustomCamera; ldnull; call op_Equality; brfalse" so the
        /// branch consumes our gate result instead of the camera null-test. The branch itself (and its
        /// target) is untouched, so the freeze path is only bypassed when the gate says so.
        /// </summary>
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            bool patched = false;

            for (int i = 0; i < list.Count - 3 && !patched; i++)
            {
                if (list[i].opcode == OpCodes.Call && list[i].operand is MethodInfo m1 && m1.Name == "get_CustomCamera"
                    && list[i + 1].opcode == OpCodes.Ldnull
                    && list[i + 2].opcode == OpCodes.Call && list[i + 2].operand is MethodInfo m2 && m2.Name == "op_Equality"
                    && (list[i + 3].opcode == OpCodes.Brfalse || list[i + 3].opcode == OpCodes.Brfalse_S))
                {
                    list[i].opcode = OpCodes.Ldarg_0;
                    list[i].operand = null;
                    list[i + 1].opcode = OpCodes.Call;
                    list[i + 1].operand = GateMethod;
                    list.RemoveAt(i + 2);
                    patched = true;
                }
            }

            if (!patched)
            {
                Log($"{nameof(Patch_MissionScreen)}: no CustomCamera null-check found in HandleUserInput - input freeze NOT gated", LogLevel.Warning);
            }

            return list;
        }
    }
}
#endif