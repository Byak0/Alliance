using Alliance.Common.Patch.Utilities;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    /// <summary>
    /// Patch the GameNetworkMessage WriteBannerCodeToPacket methods to retrieve banner code from culture instead.
    /// </summary>
    class Patch_GameNetworkMessage
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_GameNetworkMessage));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                Harmony.Patch(
                    typeof(GameNetworkMessage).GetMethod(nameof(GameNetworkMessage.ReadBannerCodeFromPacket),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_GameNetworkMessage).GetMethod(
                        nameof(Prefix_ReadBannerCodeFromPacket), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(GameNetworkMessage).GetMethod(nameof(GameNetworkMessage.WriteBannerCodeToPacket),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_GameNetworkMessage).GetMethod(
                        nameof(Prefix_WriteBannerCodeToPacket), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_GameNetworkMessage)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch ReadBannerCodeFromPacket and read the banner code from the culture name.
        /// </summary>
        public static bool Prefix_ReadBannerCodeFromPacket(ref string __result, ref bool bufferReadValid)
        {
            uint color = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
            uint color2 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref bufferReadValid);
            string culture = GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
            __result = BannerToCultureHelper.GetBannerCodeFromCulture(culture, color, color2);

            // Return false to skip original
            return false;
        }

        /// <summary>
        /// Patch WriteBannerCodeToPacket and send the culture name instead of the banner code.
        /// </summary>
        public static bool Prefix_WriteBannerCodeToPacket(string bannerCode)
        {
            if (bannerCode.IsEmpty())
            {
                GameNetworkMessage.WriteUintToPacket(4286777352, CompressionBasic.ColorCompressionInfo);
                GameNetworkMessage.WriteUintToPacket(4286777352, CompressionBasic.ColorCompressionInfo);
            }
            else
            {
                // Extract colors from banner code
                Banner banner = new Banner(bannerCode);
                uint colorId = banner.GetPrimaryColor();
                uint colorId2 = banner.GetFirstIconColor();
                GameNetworkMessage.WriteUintToPacket(colorId, CompressionBasic.ColorCompressionInfo);
                GameNetworkMessage.WriteUintToPacket(colorId2, CompressionBasic.ColorCompressionInfo);
            }
            string culture = BannerToCultureHelper.GetCultureFromBannerCode(bannerCode);
            if (culture.IsEmpty())
            {
                GameNetworkMessage.WriteStringToPacket("vlandia");
            }
            else
            {
                GameNetworkMessage.WriteStringToPacket(culture);
            }

            // Return false to skip original
            return false;
        }
    }
}