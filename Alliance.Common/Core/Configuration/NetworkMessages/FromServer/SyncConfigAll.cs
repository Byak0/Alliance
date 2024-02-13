using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.Utilities;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Core.Configuration.NetworkMessages.FromServer
{
    /// <summary>
    /// NetworkMessage to synchronize whole config between server and clients.    
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncConfigAll : GameNetworkMessage
    {
        public Config Config { get; set; }

        public SyncConfigAll() { }

        public SyncConfigAll(Config config)
        {
            Config = config;
        }

        protected override void OnWrite()
        {
            foreach (var field in ConfigManager.Instance.ConfigFields)
            {
                FieldInfo fieldInfo = field.Value;
                object fieldValue = fieldInfo.GetValue(Config);

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

        protected override bool OnRead()
        {
            Config = new Config();
            bool bufferReadValid = true;

            foreach (var field in ConfigManager.Instance.ConfigFields)
            {
                FieldInfo fieldInfo = field.Value;

                if (fieldInfo.FieldType == typeof(bool))
                {
                    fieldInfo.SetValue(Config, ReadBoolFromPacket(ref bufferReadValid));
                }
                else if (fieldInfo.FieldType == typeof(int))
                {
                    fieldInfo.SetValue(Config, ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid));
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    fieldInfo.SetValue(Config, ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid));
                }
                else if (fieldInfo.FieldType == typeof(string))
                {
                    int index = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
                    if (index > -1)
                    {
                        fieldInfo.SetValue(Config, DefaultConfig.GetAvailableValuesForOption(fieldInfo).ElementAtOrDefault(index));
                    }
                }
            }

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync mod configuration (all fields)";
        }
    }
}