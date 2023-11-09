using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    /// <summary>
    /// MultiplayerOptionsInitial and MultiplayerOptionsImmediate messages are using the MultiplayerOptions enum BoundMin/Max as compression info.
    /// Since we use much higher values than the BoundMax, we need this patch to force the use of correct Compression info.
    /// </summary>
    class Patch_MultiplayerOptionsInitial
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MultiplayerOptionsInitial));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MultiplayerOptionsInitial).GetMethod("OnRead",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerOptionsInitial).GetMethod(
                        nameof(Prefix_OnRead), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MultiplayerOptionsInitial).GetMethod("OnWrite",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerOptionsInitial).GetMethod(
                        nameof(Prefix_OnWrite), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MultiplayerOptionsInitial)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Use the correct CompressionInfo to receive options from server.
        /// </summary>
        public static bool Prefix_OnRead(ref bool __result, ref List<MultiplayerOptions.MultiplayerOption> ____optionList)
        {
            bool flag = true;
            ____optionList = new List<MultiplayerOptions.MultiplayerOption>();
            for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
            {
                MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
                if (optionProperty.Replication == MultiplayerOptionsProperty.ReplicationOccurrence.AtMapLoad)
                {
                    MultiplayerOptions.MultiplayerOption multiplayerOption = MultiplayerOptions.MultiplayerOption.CreateMultiplayerOption(optionType);

                    // Use the correct Compression Info for the options we edited
                    if (optionType == MultiplayerOptions.OptionType.MaxNumberOfPlayers)
                    {
                        multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MaxNumberOfPlayersCompressionInfo, ref flag));
                    }
                    else if (optionType == MultiplayerOptions.OptionType.RoundTimeLimit)
                    {
                        multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(CompressionMission.RoundTimeCompressionInfo, ref flag));
                    }
                    else if (optionType == MultiplayerOptions.OptionType.MapTimeLimit)
                    {
                        multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(CompressionBasic.MapTimeLimitCompressionInfo, ref flag));
                    }
                    else if (optionType == MultiplayerOptions.OptionType.NumberOfBotsPerFormation)
                    {
                        multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(CompressionBasic.NumberOfBotsPerFormationCompressionInfo, ref flag));
                    }
                    // Native
                    else
                    {
                        switch (optionProperty.OptionValueType)
                        {
                            case MultiplayerOptions.OptionValueType.Bool:
                                multiplayerOption.UpdateValue(GameNetworkMessage.ReadBoolFromPacket(ref flag));
                                break;
                            case MultiplayerOptions.OptionValueType.Integer:
                            case MultiplayerOptions.OptionValueType.Enum:
                                multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true), ref flag));
                                break;
                            case MultiplayerOptions.OptionValueType.String:
                                multiplayerOption.UpdateValue(GameNetworkMessage.ReadStringFromPacket(ref flag));
                                break;
                        }
                    }
                    ____optionList.Add(multiplayerOption);
                }
            }
            __result = flag;

            return false;
        }

        /// <summary>
        /// Use the correct CompressionInfo to send options to client.
        /// </summary>
        public static bool Prefix_OnWrite(ref List<MultiplayerOptions.MultiplayerOption> ____optionList)
        {
            foreach (MultiplayerOptions.MultiplayerOption multiplayerOption in ____optionList)
            {
                MultiplayerOptions.OptionType optionType = multiplayerOption.OptionType;
                MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();

                // Use the correct Compression Info for the options we edited
                if (optionType == MultiplayerOptions.OptionType.MaxNumberOfPlayers)
                {
                    GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions), CompressionBasic.MaxNumberOfPlayersCompressionInfo);
                }
                else if (optionType == MultiplayerOptions.OptionType.RoundTimeLimit)
                {
                    GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions), CompressionMission.RoundTimeCompressionInfo);
                }
                else if (optionType == MultiplayerOptions.OptionType.MapTimeLimit)
                {
                    GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions), CompressionBasic.MapTimeLimitCompressionInfo);
                }
                else if (optionType == MultiplayerOptions.OptionType.NumberOfBotsPerFormation)
                {
                    GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions), CompressionBasic.NumberOfBotsPerFormationCompressionInfo);
                }
                // Native
                else
                {
                    switch (optionProperty.OptionValueType)
                    {
                        case MultiplayerOptions.OptionValueType.Bool:
                            GameNetworkMessage.WriteBoolToPacket(optionType.GetBoolValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
                            break;
                        case MultiplayerOptions.OptionValueType.Integer:
                        case MultiplayerOptions.OptionValueType.Enum:
                            GameNetworkMessage.WriteIntToPacket(optionType.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions), new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true));
                            break;
                        case MultiplayerOptions.OptionValueType.String:
                            GameNetworkMessage.WriteStringToPacket(optionType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
                            break;
                    }
                }
            }

            return false;
        }
    }
}
