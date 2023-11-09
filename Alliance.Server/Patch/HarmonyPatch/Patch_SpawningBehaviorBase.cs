using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Server.Patch.HarmonyPatch
{
    public static class Patch_SpawningBehaviorBase
    {
        private static readonly Harmony Harmony = new Harmony(nameof(Patch_SpawningBehaviorBase));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched) return false;
                _patched = true;

                Harmony.Patch(
                    typeof(SpawningBehaviorBase).GetMethod(nameof(SpawningBehaviorBase.OnTick),
                        BindingFlags.Instance | BindingFlags.Public),
                    transpiler: new HarmonyMethod(typeof(Patch_SpawningBehaviorBase).GetMethod(
                        nameof(Transpiler), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_SpawningBehaviorBase)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Prevents BannerBearerCharacter from using alternative equipment
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            MethodInfo bannerBearerCharacterGetter = typeof(MPHeroClass).GetProperty("BannerBearerCharacter").GetGetMethod(nonPublic: true);
            MethodInfo stringIdGetter = typeof(MBObjectBase).GetProperty("StringId").GetGetMethod();

            bool patched = false;
            Log($"{nameof(Patch_SpawningBehaviorBase)} - Transpiler looking through IL code...", LogLevel.Information);

            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldloc_S && (codes[i].operand as LocalBuilder)?.LocalIndex == 19) // Check if enumerable2 != null
                {
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldloc_S, 11)); // Load basicCharacterObject
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Callvirt, stringIdGetter)); // Call get_StringId on basicCharacterObject
                    codes.Insert(i + 3, new CodeInstruction(OpCodes.Ldloc_3)); // Load mpheroClassForPeer
                    codes.Insert(i + 4, new CodeInstruction(OpCodes.Callvirt, bannerBearerCharacterGetter)); // Call get_BannerBearerCharacter
                    codes.Insert(i + 5, new CodeInstruction(OpCodes.Callvirt, stringIdGetter)); // Call get_StringId on BannerBearerCharacter
                    codes.Insert(i + 6, new CodeInstruction(OpCodes.Ceq)); // Compare basicCharacterObject.StringId == BannerBearerCharacter.StringId
                    codes.Insert(i + 7, new CodeInstruction(OpCodes.Ldc_I4_0)); // Load the constant 0 (false)
                    codes.Insert(i + 8, new CodeInstruction(OpCodes.Ceq)); // Check if (basicCharacterObject.StringId == BannerBearerCharacter.StringId) == false
                    codes.Insert(i + 9, new CodeInstruction(OpCodes.And)); // Perform a logical AND with the result of enumerable2 != null
                    patched = true;
                }
            }

            if (patched) Log($"{nameof(Patch_SpawningBehaviorBase)} - Transpiler applied successfully.", LogLevel.Information);
            else Log($"{nameof(Patch_SpawningBehaviorBase)} - Transpiler failed to apply", LogLevel.Error);

            return codes.AsEnumerable();
        }
    }
}
