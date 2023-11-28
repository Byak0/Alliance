using HarmonyLib;
using System;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_WidgetsMultiplayerHelper
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_WidgetsMultiplayerHelper));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                //Harmony.Patch(
                //    typeof(WidgetsMultiplayerHelper).GetMethod(nameof(WidgetsMultiplayerHelper.GetFactionColorCode),
                //        BindingFlags.Static | BindingFlags.Public),
                //    prefix: new HarmonyMethod(typeof(Patch_WidgetsMultiplayerHelper).GetMethod(
                //        nameof(Prefix_GetFactionColorCode), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_WidgetsMultiplayerHelper)}", LogLevel.Error);
                Log(e.Message, LogLevel.Error);
                return false;
            }

            return true;
        }

        // Use faction code color instead of hard fixed code
        //public static bool Prefix_GetFactionColorCode(ref string __result, string lowercaseFactionCode, bool useSecondary)
        //{
        //    __result = Color.FromUint(useSecondary ? Factions.Instance.AvailableCultures[lowercaseFactionCode].ForegroundColor1 : Factions.Instance.AvailableCultures[lowercaseFactionCode].BackgroundColor1).ToString();

        //    return false;
        //}

        /* Original method
        public static string GetFactionColorCode(string lowercaseFactionCode, bool useSecondary)
		{
			if (useSecondary)
			{
				if (lowercaseFactionCode == "aserai")
				{
					return "#4E1A13FF";
				}
				if (lowercaseFactionCode == "battania")
				{
					return "#F3F3F3FF";
				}
				if (lowercaseFactionCode == "empire")
				{
					return "#F1C232FF";
				}
				if (lowercaseFactionCode == "khuzait")
				{
					return "#FFE9D4FF";
				}
				if (lowercaseFactionCode == "sturgia")
				{
					return "#D9D9D9FF";
				}
				if (!(lowercaseFactionCode == "vlandia"))
				{
					return "#000000FF";
				}
				return "#F4CA14FF";
			}
			else
			{
				if (lowercaseFactionCode == "aserai")
				{
					return "#CC8324FF";
				}
				if (lowercaseFactionCode == "battania")
				{
					return "#335F22FF";
				}
				if (lowercaseFactionCode == "empire")
				{
					return "#802463FF";
				}
				if (lowercaseFactionCode == "khuzait")
				{
					return "#418174FF";
				}
				if (lowercaseFactionCode == "sturgia")
				{
					return "#2A5599FF";
				}
				if (!(lowercaseFactionCode == "vlandia"))
				{
					return "#FFFFFFFF";
				}
				return "#8D291AFF";
			}
		}*/
    }
}
