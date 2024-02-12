using Alliance.Common.Patch.Utilities;
using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    /// <summary>
    /// Patch the AddTeam NetworkMessage to retrieve banner code from culture instead.
    /// </summary>
    class Patch_AddTeam
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_AddTeam));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;

                _patched = true;

                Harmony.Patch(
                    typeof(AddTeam).GetMethod("OnRead",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_AddTeam).GetMethod(
                        nameof(Prefix_OnRead), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(AddTeam).GetMethod("OnWrite",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_AddTeam).GetMethod(
                        nameof(Prefix_OnWrite), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_AddTeam)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Patch OnRead and read the banner code from the culture name.
        /// </summary>
        public static bool Prefix_OnRead(AddTeam __instance, ref bool __result)
        {
            bool flag = true;

            int teamIndex = GameNetworkMessage.ReadTeamIndexFromPacket(ref flag);
            BattleSideEnum side = (BattleSideEnum)GameNetworkMessage.ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref flag);
            uint color = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref flag);
            uint color2 = GameNetworkMessage.ReadUintFromPacket(CompressionBasic.ColorCompressionInfo, ref flag);
            string culture = GameNetworkMessage.ReadStringFromPacket(ref flag);
            string bannerCode = BannerToCultureHelper.GetBannerCodeFromCulture(culture, color, color2);
            bool isPlayerGeneral = GameNetworkMessage.ReadBoolFromPacket(ref flag);
            bool isPlayerSergeant = GameNetworkMessage.ReadBoolFromPacket(ref flag);

            typeof(AddTeam).GetProperty(nameof(__instance.TeamIndex)).SetValue(__instance, teamIndex);
            typeof(AddTeam).GetProperty(nameof(__instance.Side)).SetValue(__instance, side);
            typeof(AddTeam).GetProperty(nameof(__instance.Color)).SetValue(__instance, color);
            typeof(AddTeam).GetProperty(nameof(__instance.Color2)).SetValue(__instance, color2);
            typeof(AddTeam).GetProperty(nameof(__instance.BannerCode)).SetValue(__instance, bannerCode);
            typeof(AddTeam).GetProperty(nameof(__instance.IsPlayerGeneral)).SetValue(__instance, isPlayerGeneral);
            typeof(AddTeam).GetProperty(nameof(__instance.IsPlayerSergeant)).SetValue(__instance, isPlayerSergeant);

            __result = flag;

            // Return false to skip original
            return false;
        }

        /// <summary>
        /// Patch OnWrite and send the culture name instead of the banner code.
        /// </summary>
        public static bool Prefix_OnWrite(AddTeam __instance)
        {
            GameNetworkMessage.WriteTeamIndexToPacket(__instance.TeamIndex);
            GameNetworkMessage.WriteIntToPacket((int)__instance.Side, CompressionMission.TeamSideCompressionInfo);
            GameNetworkMessage.WriteUintToPacket(__instance.Color, CompressionBasic.ColorCompressionInfo);
            GameNetworkMessage.WriteUintToPacket(__instance.Color2, CompressionBasic.ColorCompressionInfo);
            GameNetworkMessage.WriteStringToPacket(BannerToCultureHelper.GetCultureFromBannerCode(__instance.BannerCode));
            GameNetworkMessage.WriteBoolToPacket(__instance.IsPlayerGeneral);
            GameNetworkMessage.WriteBoolToPacket(__instance.IsPlayerSergeant);

            // Return false to skip original
            return false;
        }
    }
}
