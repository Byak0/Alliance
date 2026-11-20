using Alliance.Common.Core.Configuration.Models;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// Network serialization utilities for MultiplayerOptions.
	/// Centralizes the logic for options that use extended bounds beyond native limits.
	/// </summary>
	public static class MultiplayerOptionsSerializer
	{
		public static object GetOptionValue(this MultiplayerOptions.OptionType optionType)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			return optionProperty.OptionValueType switch
			{
				MultiplayerOptions.OptionValueType.Bool => optionType.GetBoolValue(),
				MultiplayerOptions.OptionValueType.Integer or MultiplayerOptions.OptionValueType.Enum => optionType.GetIntValue(),
				MultiplayerOptions.OptionValueType.String => optionType.GetStrValue(),
				_ => null
			};
		}

		/// <summary>
		/// Write a single option value to the network packet using the correct compression.
		/// </summary>
		public static void WriteOption(MultiplayerOptions.OptionType optionType, object value)
		{
			// Check if we have custom compression first
			CompressionInfo.Integer? customCompression = TWConfig.GetCustomCompressionInfo(optionType);
			if (customCompression.HasValue)
			{
				GameNetworkMessage.WriteIntToPacket((int)value, customCompression.Value);
				return;
			}

			// Otherwise, use standard compression
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			switch (optionProperty.OptionValueType)
			{
				case MultiplayerOptions.OptionValueType.Bool:
					GameNetworkMessage.WriteBoolToPacket((bool)value);
					break;
				case MultiplayerOptions.OptionValueType.Integer:
				case MultiplayerOptions.OptionValueType.Enum:
					GameNetworkMessage.WriteIntToPacket((int)value,
						new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true));
					break;
				case MultiplayerOptions.OptionValueType.String:
					GameNetworkMessage.WriteStringToPacket((string)value);
					break;
			}
		}

		/// <summary>
		/// Read a single option value from the network packet using the correct compression.
		/// </summary>
		public static object ReadOption(MultiplayerOptions.OptionType optionType, ref bool bufferReadValid)
		{
			CompressionInfo.Integer? customCompression = TWConfig.GetCustomCompressionInfo(optionType);
			if (customCompression.HasValue)
			{
				return GameNetworkMessage.ReadIntFromPacket(customCompression.Value, ref bufferReadValid);
			}

			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			return optionProperty.OptionValueType switch
			{
				MultiplayerOptions.OptionValueType.Bool =>
					GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid),
				MultiplayerOptions.OptionValueType.Integer or MultiplayerOptions.OptionValueType.Enum =>
					GameNetworkMessage.ReadIntFromPacket(
						new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true),
						ref bufferReadValid),
				MultiplayerOptions.OptionValueType.String =>
					GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid),
				_ => null
			};
		}

		/// <summary>
		/// Read an option value from the network packet and update a MultiplayerOption with the correct type.
		/// </summary>
		public static void ReadOptionAndUpdate(MultiplayerOptions.MultiplayerOption multiplayerOption, ref bool bufferReadValid)
		{
			MultiplayerOptions.OptionType optionType = multiplayerOption.OptionType;
			CompressionInfo.Integer? customCompression = TWConfig.GetCustomCompressionInfo(optionType);

			if (customCompression.HasValue)
			{
				multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(customCompression.Value, ref bufferReadValid));
				return;
			}

			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			switch (optionProperty.OptionValueType)
			{
				case MultiplayerOptions.OptionValueType.Bool:
					multiplayerOption.UpdateValue(GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid));
					break;
				case MultiplayerOptions.OptionValueType.Integer:
				case MultiplayerOptions.OptionValueType.Enum:
					multiplayerOption.UpdateValue(GameNetworkMessage.ReadIntFromPacket(
						new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true),
						ref bufferReadValid));
					break;
				case MultiplayerOptions.OptionValueType.String:
					multiplayerOption.UpdateValue(GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid));
					break;
			}
		}

		public static void WriteModOption(FieldInfo fieldInfo, object fieldValue)
		{
			if (fieldInfo.FieldType == typeof(bool))
			{
				GameNetworkMessage.WriteBoolToPacket((bool)fieldValue);
			}
			else if (fieldInfo.FieldType == typeof(int))
			{
				GameNetworkMessage.WriteIntToPacket((int)fieldValue, CompressionHelper.DefaultIntValueCompressionInfo);
			}
			else if (fieldInfo.FieldType == typeof(float))
			{
				GameNetworkMessage.WriteFloatToPacket((float)fieldValue, CompressionHelper.DefaultFloatValueCompressionInfo);
			}
			else if (fieldInfo.FieldType == typeof(string))
			{
				ConfigPropertyAttribute attribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();
				if (attribute.DataType == AllianceData.DataTypes.None)
				{
					GameNetworkMessage.WriteStringToPacket((string)fieldValue);
				}
				else
				{
					int index = attribute.PossibleValues.FindIndex(item => item == (string)fieldValue);
					GameNetworkMessage.WriteIntToPacket(index, CompressionHelper.DefaultIntValueCompressionInfo);
				}
			}
		}

		public static void ReadModOption(FieldInfo fieldInfo, Config modOptions, ref bool bufferReadValid)
		{
			if (fieldInfo.FieldType == typeof(bool))
			{
				fieldInfo.SetValue(modOptions, GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid));
			}
			else if (fieldInfo.FieldType == typeof(int))
			{
				fieldInfo.SetValue(modOptions, GameNetworkMessage.ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid));
			}
			else if (fieldInfo.FieldType == typeof(float))
			{
				float roundedValue = CompressionHelper.ReadRoundedFloat(CompressionHelper.DefaultFloatValueCompressionInfo, CompressionHelper.DefaultFloatValueDigits, ref bufferReadValid);
				fieldInfo.SetValue(modOptions, roundedValue);
			}
			else if (fieldInfo.FieldType == typeof(string))
			{
				ConfigPropertyAttribute attribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();
				if (attribute.DataType == AllianceData.DataTypes.None)
				{
					fieldInfo.SetValue(modOptions, GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid));
				}
				else
				{
					int index = GameNetworkMessage.ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
					fieldInfo.SetValue(modOptions, attribute.PossibleValues.ElementAtOrDefault(index));
				}
			}
		}

		public static object ReadModOption(FieldInfo fieldInfo, ref bool bufferReadValid)
		{
			if (fieldInfo.FieldType == typeof(bool))
			{
				return GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
			}
			else if (fieldInfo.FieldType == typeof(int))
			{
				return GameNetworkMessage.ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			}
			else if (fieldInfo.FieldType == typeof(float))
			{
				return CompressionHelper.ReadRoundedFloat(CompressionHelper.DefaultFloatValueCompressionInfo, CompressionHelper.DefaultFloatValueDigits, ref bufferReadValid);				
			}
			else if (fieldInfo.FieldType == typeof(string))
			{
				ConfigPropertyAttribute attribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();
				if (attribute.DataType == AllianceData.DataTypes.None)
				{
					return GameNetworkMessage.ReadStringFromPacket(ref bufferReadValid);
				}
				else
				{
					int index = GameNetworkMessage.ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
					return attribute.PossibleValues.ElementAtOrDefault(index);
				}
			}
			return null;
		}
	}
}