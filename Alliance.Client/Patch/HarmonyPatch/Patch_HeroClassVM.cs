using Alliance.Common.Core.Security.Extension;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_HeroClassVM
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_HeroClassVM));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(HeroClassVM).GetMethod(nameof(HeroClassVM.RefreshValues),
                        BindingFlags.Instance | BindingFlags.Public),
                    postfix: new HarmonyMethod(typeof(Patch_HeroClassVM).GetMethod(
                        nameof(Postfix_RefreshValues), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_HeroClassVM)}", LogLevel.Error);
                Log(e.Message, LogLevel.Error);
                return false;
            }

            return true;
        }

        // If player is not an officer, show basic troop name instead of hero name in LobbyEquipment
        public static void Postfix_RefreshValues(HeroClassVM __instance)
        {
            if (!GameNetwork.MyPeer.IsOfficer())
            {
                __instance.Name = __instance.HeroClass.TroopName.ToString();
            }
        }

        /* Original method
        public override void RefreshValues()
        {
            base.RefreshValues();
            Name = HeroClass.HeroName.ToString();
            Perks.ApplyActionOnAllItems(delegate (HeroPerkVM x)
            {
                x.RefreshValues();
            });
        }*/
    }
}
