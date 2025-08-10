using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.Utilities;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
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
		public Type FieldType { get => ConfigManager.Instance.ConfigFields[FieldIndex].FieldType; }

		public SyncConfigField() { }

		public SyncConfigField(int fieldIndex, object fieldValue)
		{
			FieldIndex = fieldIndex;
			FieldValue = fieldValue;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(FieldIndex, new CompressionInfo.Integer(0, ConfigManager.Instance.ConfigFields.Count, true));

			if (FieldType == typeof(bool))
			{
				WriteBoolToPacket((bool)FieldValue);
			}
			else if (FieldType == typeof(int))
			{
				WriteIntToPacket((int)FieldValue, CompressionHelper.DefaultIntValueCompressionInfo);
			}
			else if (FieldType == typeof(float))
			{
				WriteFloatToPacket((float)FieldValue, CompressionHelper.DefaultFloatValueCompressionInfo);
			}
			else if (FieldType == typeof(string))
			{
				ConfigPropertyAttribute attribute = ConfigManager.Instance.ConfigFields[FieldIndex].GetCustomAttribute<ConfigPropertyAttribute>();
				if (attribute.DataType == AllianceData.DataTypes.None)
				{
					WriteStringToPacket((string)FieldValue);
				}
				else
				{
					int index = attribute.PossibleValues.FindIndex(item => item == (string)FieldValue);
					WriteIntToPacket(index, CompressionHelper.DefaultIntValueCompressionInfo);
				}
			}
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;

			FieldIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, ConfigManager.Instance.ConfigFields.Count, true), ref bufferReadValid);

			if (FieldType == typeof(bool))
			{
				FieldValue = ReadBoolFromPacket(ref bufferReadValid);
			}
			else if (FieldType == typeof(int))
			{
				FieldValue = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			}
			else if (FieldType == typeof(float))
			{
				FieldValue = ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid);
			}
			else if (FieldType == typeof(string))
			{
				ConfigPropertyAttribute attribute = ConfigManager.Instance.ConfigFields[FieldIndex].GetCustomAttribute<ConfigPropertyAttribute>();
				if (attribute.DataType == AllianceData.DataTypes.None)
				{
					FieldValue = ReadStringFromPacket(ref bufferReadValid);
				}
				else
				{
					int index = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
					FieldValue = attribute.PossibleValues.ElementAtOrDefault(index);
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
			return "Sync mod configuration " + ConfigManager.Instance.ConfigFields[FieldIndex].Name + " => " + FieldValue;
		}
	}
}