using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.GameModeMenu.NetworkMessages.FromClient
{
    /// <summary>
    /// NetworkMessage to request a new GameMode poll to the server.
    /// Contains all options related to that GameMode.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class GameModePollRequestMessage : GameNetworkMessage
    {
        public List<MultiplayerOptions.MultiplayerOption> NativeOptions { get; set; }
        public Config ModOptions { get; set; }
        public bool SkipPoll { get; private set; }

        public GameModePollRequestMessage() { }

        public GameModePollRequestMessage(List<MultiplayerOptions.MultiplayerOption> optionList, Config config, bool skipPoll)
        {
            NativeOptions = optionList;
            ModOptions = config;
            SkipPoll = skipPoll;
        }

        public MultiplayerOptions.MultiplayerOption GetOption(MultiplayerOptions.OptionType optionType)
        {
            return NativeOptions.First((x) => x.OptionType == optionType);
        }

        protected override void OnWrite()
        {
            WriteNativeOptions();
            WriteModOptions();
            WriteBoolToPacket(SkipPoll);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            ReadNativeOptions(ref bufferReadValid);
            ReadModOptions(ref bufferReadValid);
            SkipPoll = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        private void WriteNativeOptions()
        {
            foreach (MultiplayerOptions.MultiplayerOption multiplayerOption in NativeOptions)
            {
                MultiplayerOptions.OptionType optionType = multiplayerOption.OptionType;
                MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
                if (optionType == MultiplayerOptions.OptionType.MaxNumberOfPlayers)
                {
                    multiplayerOption.GetValue(out int maxNumberOfPlayers);
                    WriteIntToPacket(maxNumberOfPlayers, CompressionBasic.MaxNumberOfPlayersCompressionInfo);
                }
                else if (optionType == MultiplayerOptions.OptionType.RoundTimeLimit)
                {
                    multiplayerOption.GetValue(out int roundTimeLimit);
                    WriteIntToPacket(roundTimeLimit, CompressionMission.RoundTimeCompressionInfo);
                }
                else if (optionType == MultiplayerOptions.OptionType.MapTimeLimit)
                {
                    multiplayerOption.GetValue(out int mapTimeLimit);
                    WriteIntToPacket(mapTimeLimit, CompressionBasic.MapTimeLimitCompressionInfo);
                }
                else if (optionType == MultiplayerOptions.OptionType.NumberOfBotsPerFormation)
                {
                    multiplayerOption.GetValue(out int numberOfBotsPerFormation);
                    WriteIntToPacket(numberOfBotsPerFormation, CompressionBasic.NumberOfBotsPerFormationCompressionInfo);
                }
                else
                {
                    switch (optionProperty.OptionValueType)
                    {
                        case MultiplayerOptions.OptionValueType.Bool:
                            multiplayerOption.GetValue(out bool flag);
                            WriteBoolToPacket(flag);
                            break;
                        case MultiplayerOptions.OptionValueType.Integer:
                        case MultiplayerOptions.OptionValueType.Enum:
                            multiplayerOption.GetValue(out int num);
                            WriteIntToPacket(num, new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true));
                            break;
                        case MultiplayerOptions.OptionValueType.String:
                            multiplayerOption.GetValue(out string text);
                            WriteStringToPacket(text);
                            break;
                    }
                }
            }
        }

        private void WriteModOptions()
        {
            foreach (var field in ConfigManager.Instance.ConfigFields)
            {
                FieldInfo fieldInfo = field.Value;
                object fieldValue = fieldInfo.GetValue(ModOptions);

                if (fieldInfo.FieldType == typeof(bool))
                {
                    WriteBoolToPacket((bool)fieldValue);
                }
                else if (fieldInfo.FieldType == typeof(int))
                {
                    WriteIntToPacket((int)fieldValue, CompressionHelper.DefaultIntValueCompressionInfo);
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    WriteFloatToPacket((float)fieldValue, CompressionHelper.DefaultFloatValueCompressionInfo);
                }
                else if (fieldInfo.FieldType == typeof(string))
                {
                    int index = DefaultConfig.GetAvailableValuesForOption(fieldInfo).FindIndex(item => item == (string)fieldValue);
                    WriteIntToPacket(index, CompressionHelper.DefaultIntValueCompressionInfo);
                }
            }
        }

        private void ReadNativeOptions(ref bool bufferReadValid)
        {
            NativeOptions = new List<MultiplayerOptions.MultiplayerOption>();
            for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
            {
                MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
                MultiplayerOptions.MultiplayerOption multiplayerOption = MultiplayerOptions.MultiplayerOption.CreateMultiplayerOption(optionType);
                if (optionType == MultiplayerOptions.OptionType.MaxNumberOfPlayers)
                {
                    multiplayerOption.UpdateValue(ReadIntFromPacket(CompressionBasic.MaxNumberOfPlayersCompressionInfo, ref bufferReadValid));
                }
                else if (optionType == MultiplayerOptions.OptionType.RoundTimeLimit)
                {
                    multiplayerOption.UpdateValue(ReadIntFromPacket(CompressionMission.RoundTimeCompressionInfo, ref bufferReadValid));
                }
                else if (optionType == MultiplayerOptions.OptionType.MapTimeLimit)
                {
                    multiplayerOption.UpdateValue(ReadIntFromPacket(CompressionBasic.MapTimeLimitCompressionInfo, ref bufferReadValid));
                }
                else if (optionType == MultiplayerOptions.OptionType.NumberOfBotsPerFormation)
                {
                    multiplayerOption.UpdateValue(ReadIntFromPacket(CompressionBasic.NumberOfBotsPerFormationCompressionInfo, ref bufferReadValid));
                }
                else
                {
                    switch (optionProperty.OptionValueType)
                    {
                        case MultiplayerOptions.OptionValueType.Bool:
                            multiplayerOption.UpdateValue(ReadBoolFromPacket(ref bufferReadValid));
                            break;
                        case MultiplayerOptions.OptionValueType.Integer:
                        case MultiplayerOptions.OptionValueType.Enum:
                            multiplayerOption.UpdateValue(ReadIntFromPacket(new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true), ref bufferReadValid));
                            break;
                        case MultiplayerOptions.OptionValueType.String:
                            multiplayerOption.UpdateValue(ReadStringFromPacket(ref bufferReadValid));
                            break;
                    }
                }
                NativeOptions.Add(multiplayerOption);
            }
        }

        private void ReadModOptions(ref bool bufferReadValid)
        {
            ModOptions = new Config();
            foreach (var field in ConfigManager.Instance.ConfigFields)
            {
                FieldInfo fieldInfo = field.Value;

                if (fieldInfo.FieldType == typeof(bool))
                {
                    fieldInfo.SetValue(ModOptions, ReadBoolFromPacket(ref bufferReadValid));
                }
                else if (fieldInfo.FieldType == typeof(int))
                {
                    fieldInfo.SetValue(ModOptions, ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid));
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    fieldInfo.SetValue(ModOptions, ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid));
                }
                else if (fieldInfo.FieldType == typeof(string))
                {
                    int index = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
                    if (index > -1)
                    {
                        fieldInfo.SetValue(ModOptions, DefaultConfig.GetAvailableValuesForOption(fieldInfo).ElementAtOrDefault(index));
                    }
                }
            }
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Request a new game mode poll to the server";
        }
    }
}