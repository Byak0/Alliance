using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.Core.Configuration
{
    /// <summary>
    /// Manage configuration, update values and ensure synchronization with clients
    /// </summary>  
    public class ConfigManager
    {
        /// <summary>
        /// List of fields from Config class. Used to identify fields with their index and reduce network load in synchronization.
        /// </summary>        
        public readonly Dictionary<int, FieldInfo> ConfigFields = new();

        private static ConfigManager _instance;

        public static ConfigManager Instance => _instance;

        static ConfigManager()
        {
            _instance = new ConfigManager();
        }

        private ConfigManager()
        {
            FieldInfo[] configFields = typeof(DefaultConfig).GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < configFields.Length; i++)
            {
                ConfigFields.Add(i, configFields[i]);
            }
        }

        /// <summary>
        /// Update config from a deserialized version of the class.
        /// Compare both versions and update only the difference.
        /// </summary>
        public void UpdateConfigFromDeserialized(Config deserializedConfig, bool synchronize = false)
        {
            foreach (KeyValuePair<int, FieldInfo> field in ConfigFields)
            {
                var deserializedValue = field.Value.GetValue(deserializedConfig);
                var actualValue = field.Value.GetValue(Config.Instance);
                if (!actualValue.Equals(deserializedValue))
                {
                    UpdateConfigField(field.Key, deserializedValue);
                    // If sync just got activated, send server config to everyone
                    if (field.Value.Name == nameof(Config.SyncConfig) && Config.Instance.SyncConfig)
                    {
                        SendConfigToAllPeers();
                    }
                    else if (synchronize)
                    {
                        SyncConfigField(field.Key, deserializedValue);
                    }
                }
            }
        }

        public void SendConfigToPeer(NetworkCommunicator networkPeer)
        {
            GameNetwork.BeginModuleEventAsServer(networkPeer);
            GameNetwork.WriteMessage(new SyncConfigAll(Config.Instance));
            GameNetwork.EndModuleEventAsServer();
        }

        public void SendConfigToAllPeers()
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SyncConfigAll(Config.Instance));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
        }

        public void UpdateConfigField(int fieldIndex, object fieldValue)
        {
            var oldValue = ConfigFields[fieldIndex].GetValue(Config.Instance);
            ConfigFields[fieldIndex].SetValue(Config.Instance, fieldValue);
            Log($"Updated config {ConfigFields[fieldIndex].Name} : {oldValue} => {fieldValue}", LogLevel.Debug);
        }

        /// <summary>
        /// Synchronize given config field with all players.
        /// </summary>
        public void SyncConfigField(int fieldIndex, object fieldValue)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SyncConfigField(fieldIndex, fieldValue));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        /// <summary>
        /// Retrieve a copy of current list of native options.
        /// </summary>
        public List<MultiplayerOption> GetNativeOptionsCopy()
        {
            List<MultiplayerOption> nativeOptions = new();
            for (OptionType optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
            {
                MultiplayerOption option = MultiplayerOption.CreateMultiplayerOption(optionType);
                MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
                switch (optionProperty.OptionValueType)
                {
                    case OptionValueType.Bool:
                        {
                            MultiplayerOptions.Instance.GetOptionFromOptionType(optionType, MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out bool flag);
                            option.UpdateValue(flag);
                            break;
                        }
                    case OptionValueType.Integer:
                    case OptionValueType.Enum:
                        {
                            MultiplayerOptions.Instance.GetOptionFromOptionType(optionType, MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out int num);
                            option.UpdateValue(num);
                            break;
                        }
                    case OptionValueType.String:
                        {
                            MultiplayerOptions.Instance.GetOptionFromOptionType(optionType, MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out string text);
                            option.UpdateValue(text);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                nativeOptions.Add(option);
            }
            return nativeOptions;
        }

        /// <summary>
        /// Apply given list of native options.
        /// </summary>
        public void ApplyNativeOptions(List<MultiplayerOption> optionList)
        {
            for (OptionType optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
            {
                MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
                switch (optionProperty.OptionValueType)
                {
                    case OptionValueType.Bool:
                        {
                            optionList.First((x) => x.OptionType == optionType).GetValue(out bool flag);
                            optionType.SetValue(flag, MultiplayerOptionsAccessMode.CurrentMapOptions);
                            break;
                        }
                    case OptionValueType.Integer:
                    case OptionValueType.Enum:
                        {
                            optionList.First((x) => x.OptionType == optionType).GetValue(out int num);
                            optionType.SetValue(num, MultiplayerOptionsAccessMode.CurrentMapOptions);
                            break;
                        }
                    case OptionValueType.String:
                        {
                            optionList.First((x) => x.OptionType == optionType).GetValue(out string text);
                            optionType.SetValue(text, MultiplayerOptionsAccessMode.CurrentMapOptions);
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary>
        /// Retrieve a copy of current config.
        /// </summary>
        public Config GetModOptionsCopy()
        {
            Config configCopy = new();
            foreach (KeyValuePair<int, FieldInfo> field in ConfigFields)
            {
                field.Value.SetValue(configCopy, field.Value.GetValue(Config.Instance));
            }
            return configCopy;
        }

        /// <summary>
        /// Apply given list of mod options.
        /// </summary>
        public void ApplyModOptions(Config modOptions)
        {
            Config.Instance = modOptions;
            SendConfigToAllPeers();
        }
    }
}