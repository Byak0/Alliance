using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.Utilities;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Core.Configuration.NetworkMessages.FromServer
{
    /// <summary>
    /// NetworkMessage to synchronize single config field between server and clients.    
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncConfigField : GameNetworkMessage
    {
        public int FieldIndex { get; private set; }
        public object FieldValue { get; private set; }
        public object FieldType { get => ConfigManager.Instance.ConfigFields[FieldIndex].GetValue(Config.Instance); }

        public SyncConfigField() { }

        public SyncConfigField(int fieldIndex, object fieldValue)
        {
            FieldIndex = fieldIndex;
            FieldValue = fieldValue;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(FieldIndex, new CompressionInfo.Integer(0, ConfigManager.Instance.ConfigFields.Count, true));
            switch (FieldType)
            {
                case int intValue:
                    WriteIntToPacket(intValue, CompressionHelper.DefaultIntValueCompressionInfo);
                    break;
                case float floatValue:
                    WriteFloatToPacket(floatValue, CompressionHelper.DefaultFloatValueCompressionInfo);
                    break;
                case string stringValue:
                    WriteStringToPacket(stringValue);
                    break;
                case bool boolValue:
                    WriteBoolToPacket(boolValue);
                    break;
                default:
                    WriteStringToPacket(FieldValue.ToString());
                    break;
            }
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            FieldIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, ConfigManager.Instance.ConfigFields.Count, true), ref bufferReadValid);
            switch (FieldType)
            {
                case int:
                    FieldValue = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
                    break;
                case float:
                    FieldValue = ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid);
                    break;
                case string:
                    FieldValue = ReadStringFromPacket(ref bufferReadValid);
                    break;
                case bool:
                    FieldValue = ReadBoolFromPacket(ref bufferReadValid);
                    break;
                default:
                    FieldValue = ReadStringFromPacket(ref bufferReadValid);
                    break;
            }
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync mod configuration " + ConfigManager.Instance.ConfigFields[FieldIndex].Name + " => " + FieldValue;
        }
    }
}