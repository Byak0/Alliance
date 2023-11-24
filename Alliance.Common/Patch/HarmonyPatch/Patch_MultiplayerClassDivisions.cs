using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    class Patch_MultiplayerClassDivisions
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MultiplayerClassDivisions));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                MethodInfo originalMethod = typeof(MultiplayerClassDivisions).GetMethod("GetMPHeroClassForCharacter",
                    BindingFlags.Static | BindingFlags.Public);

                MethodInfo prefixMethod = typeof(Patch_MultiplayerClassDivisions).GetMethod(
                    nameof(Prefix_GetMPHeroClassForCharacter), BindingFlags.Static | BindingFlags.Public);

                Harmony.Patch(originalMethod, prefix: new HarmonyMethod(prefixMethod));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MultiplayerClassDivisions)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Allow Banner Bearer character to be retrieved
        public static bool Prefix_GetMPHeroClassForCharacter(ref MultiplayerClassDivisions.MPHeroClass __result, BasicCharacterObject character)
        {
            __result = MBObjectManager.Instance.GetObjectTypeList<MultiplayerClassDivisions.MPHeroClass>()
                .FirstOrDefault((x) =>
                x.HeroCharacter == character ||
                x.TroopCharacter == character ||
                x.BannerBearerCharacter == character);

            // Skip original method
            return false;
        }

        /*
         * Previous method
         *        
        public static MultiplayerClassDivisions.MPHeroClass GetMPHeroClassForCharacter(BasicCharacterObject character)
		{
			return MBObjectManager.Instance.GetObjectTypeList<MultiplayerClassDivisions.MPHeroClass>().FirstOrDefault((MultiplayerClassDivisions.MPHeroClass x) => x.HeroCharacter == character || x.TroopCharacter == character);
		}
        */
    }
}
