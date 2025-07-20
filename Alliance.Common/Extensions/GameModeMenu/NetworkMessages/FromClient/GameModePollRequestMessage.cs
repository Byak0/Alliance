using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.Utilities;
using System.Linq;
using System.Reflection;
using TaleWorlds.Library;
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
		public TWConfig NativeOptions { get; set; }
		public Config ModOptions { get; set; }
		public bool SkipPoll { get; private set; }

		public GameModePollRequestMessage() { }

		public GameModePollRequestMessage(TWConfig twConfig, Config config, bool skipPoll)
		{
			NativeOptions = twConfig;
			ModOptions = config;
			SkipPoll = skipPoll;
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
			for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
			{
				MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
				if (optionType == MultiplayerOptions.OptionType.MaxNumberOfPlayers)
				{
					WriteIntToPacket((int)NativeOptions[optionType], CompressionBasic.MaxNumberOfPlayersCompressionInfo);
				}
				else if (optionType == MultiplayerOptions.OptionType.RoundTimeLimit)
				{
					WriteIntToPacket((int)NativeOptions[optionType], CompressionMission.RoundTimeCompressionInfo);
				}
				else if (optionType == MultiplayerOptions.OptionType.MapTimeLimit)
				{
					WriteIntToPacket((int)NativeOptions[optionType], CompressionBasic.MapTimeLimitCompressionInfo);
				}
				else if (optionType == MultiplayerOptions.OptionType.NumberOfBotsPerFormation)
				{
					WriteIntToPacket((int)NativeOptions[optionType], CompressionBasic.NumberOfBotsPerFormationCompressionInfo);
				}
				else
				{
					switch (optionProperty.OptionValueType)
					{
						case MultiplayerOptions.OptionValueType.Bool:
							WriteBoolToPacket((bool)NativeOptions[optionType]);
							break;
						case MultiplayerOptions.OptionValueType.Integer:
						case MultiplayerOptions.OptionValueType.Enum:
							WriteIntToPacket((int)NativeOptions[optionType], new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true));
							break;
						case MultiplayerOptions.OptionValueType.String:
							WriteStringToPacket((string)NativeOptions[optionType]);
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
					ConfigPropertyAttribute attribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();
					if (attribute.DataType == AllianceData.DataTypes.None)
					{
						WriteStringToPacket((string)fieldValue);
					}
					else
					{
						int index = attribute.PossibleValues.FindIndex(item => item == (string)fieldValue);
						WriteIntToPacket(index, CompressionHelper.DefaultIntValueCompressionInfo);
					}
				}
			}
		}

		private void ReadNativeOptions(ref bool bufferReadValid)
		{
			NativeOptions = new TWConfig();
			for (MultiplayerOptions.OptionType optionType = MultiplayerOptions.OptionType.ServerName; optionType < MultiplayerOptions.OptionType.NumOfSlots; optionType++)
			{
				MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
				if (optionType == MultiplayerOptions.OptionType.MaxNumberOfPlayers)
				{
					NativeOptions[optionType] = ReadIntFromPacket(CompressionBasic.MaxNumberOfPlayersCompressionInfo, ref bufferReadValid);
				}
				else if (optionType == MultiplayerOptions.OptionType.RoundTimeLimit)
				{
					NativeOptions[optionType] = ReadIntFromPacket(CompressionMission.RoundTimeCompressionInfo, ref bufferReadValid);
				}
				else if (optionType == MultiplayerOptions.OptionType.MapTimeLimit)
				{
					NativeOptions[optionType] = ReadIntFromPacket(CompressionBasic.MapTimeLimitCompressionInfo, ref bufferReadValid);
				}
				else if (optionType == MultiplayerOptions.OptionType.NumberOfBotsPerFormation)
				{
					NativeOptions[optionType] = ReadIntFromPacket(CompressionBasic.NumberOfBotsPerFormationCompressionInfo, ref bufferReadValid);
				}
				else
				{
					switch (optionProperty.OptionValueType)
					{
						case MultiplayerOptions.OptionValueType.Bool:
							NativeOptions[optionType] = ReadBoolFromPacket(ref bufferReadValid);
							break;
						case MultiplayerOptions.OptionValueType.Integer:
						case MultiplayerOptions.OptionValueType.Enum:
							NativeOptions[optionType] = ReadIntFromPacket(new CompressionInfo.Integer(optionProperty.BoundsMin, optionProperty.BoundsMax, true), ref bufferReadValid);
							break;
						case MultiplayerOptions.OptionValueType.String:
							NativeOptions[optionType] = ReadStringFromPacket(ref bufferReadValid);
							break;
					}
				}
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
					ConfigPropertyAttribute attribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();
					if (attribute.DataType == AllianceData.DataTypes.None)
					{
						fieldInfo.SetValue(ModOptions, ReadStringFromPacket(ref bufferReadValid));
					}
					else
					{
						int index = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
						fieldInfo.SetValue(ModOptions, attribute.PossibleValues.ElementAtOrDefault(index));
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