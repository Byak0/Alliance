using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.NetworkMessages.FromClient;
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
		/// Validate a specific config instance.
		/// Returns true if any value was corrected.
		/// </summary>
		public bool ValidateConfigInstance(Config configToValidate)
		{
			DefaultConfig defaultConfig = new DefaultConfig();
			bool hasInvalidValues = false;
			
			foreach (KeyValuePair<int, FieldInfo> field in ConfigFields)
			{
				FieldInfo fieldInfo = field.Value;
				ConfigPropertyAttribute attribute = fieldInfo.GetCustomAttribute<ConfigPropertyAttribute>();
				
				if (attribute == null) continue;

				object currentValue = fieldInfo.GetValue(configToValidate);
				object defaultValue = fieldInfo.GetValue(defaultConfig);
				bool isValid = true;
				string validationError = null;

				// Check values are within legal bounds
				switch (currentValue)
				{
					case int intValue:
						if (intValue < attribute.MinValue || intValue > attribute.MaxValue)
						{
							isValid = false;
							validationError = $"Value {intValue} is out of range [{attribute.MinValue}, {attribute.MaxValue}]";
						}
						break;

					case float floatValue:
						if (floatValue < attribute.MinValue || floatValue > attribute.MaxValue)
						{
							isValid = false;
							validationError = $"Value {floatValue} is out of range [{attribute.MinValue}, {attribute.MaxValue}]";
						}
						break;

					case string stringValue:
						// If from a specific DataType, ensure it is a valid value
						if (attribute.DataType != AllianceData.DataTypes.None)
						{
							string[] possibleValues = attribute.PossibleValues;
							if (possibleValues != null && possibleValues.Length > 0 && !possibleValues.Contains(stringValue))
							{
								isValid = false;
								validationError = $"Value '{stringValue}' is not in allowed values: [{string.Join(", ", possibleValues)}]";
							}
						}
						break;
				}

				// Reset to default value if invalid
				if (!isValid)
				{
					Log($"Config validation failed for '{fieldInfo.Name}': {validationError}. Resetting to default: {defaultValue}", LogLevel.Warning);
					fieldInfo.SetValue(configToValidate, defaultValue);
					hasInvalidValues = true;
				}
			}
			
			return hasInvalidValues;
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

				// Only update if value has changed
				if ((actualValue == null && deserializedValue == null) ||
					(actualValue != null && actualValue.Equals(deserializedValue)))
				{
					continue;
				}

				UpdateConfigField(field.Key, deserializedValue);
				if (synchronize)
				{
					SyncConfigField(field.Key, deserializedValue);
				}
			}
		}

		public void SendMyConfigToServer(UserConfig userConfig)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new UpdateUserConfig(userConfig));
			GameNetwork.EndModuleEventAsClient();
		}

		public void SendConfigToPeer(NetworkCommunicator networkPeer)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new SyncConfigAll(Config.Instance));
			GameNetwork.EndModuleEventAsServer();
		}

		public void SendConfigToAllPeers()
		{
			Log($"Sending config to all peers", LogLevel.Debug);
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
			if (!GameNetwork.IsServer) return;
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncConfigField(fieldIndex, fieldValue));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// Retrieve a copy of current list of native options.
		/// </summary>
		public TWConfig GetNativeOptionsCopy()
		{
			TWConfig nativeOptions = new TWConfig();

			// If we are not in a multiplayer game, return default options
			if (!GameNetwork.IsMultiplayer)
			{
				return nativeOptions;
			}

			for (OptionType optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
			{
				MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
				switch (optionProperty.OptionValueType)
				{
					case OptionValueType.Bool:
						{
							MultiplayerOptions.Instance.GetOptionFromOptionType(optionType, MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out bool flag);
							nativeOptions[optionType] = flag;
							break;
						}
					case OptionValueType.Integer:
					case OptionValueType.Enum:
						{
							MultiplayerOptions.Instance.GetOptionFromOptionType(optionType, MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out int num);
							nativeOptions[optionType] = num;
							break;
						}
					case OptionValueType.String:
						{
							MultiplayerOptions.Instance.GetOptionFromOptionType(optionType, MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out string text);
							nativeOptions[optionType] = text;
							break;
						}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			return nativeOptions;
		}

		/// <summary>
		/// Apply given list of native options.
		/// </summary>
		public void ApplyNativeOptions(TWConfig optionList)
		{
			if (optionList == null)
			{
				Log("Tried to apply empty native options, skipping...", LogLevel.Warning);
				return;
			}
			for (OptionType optionType = OptionType.ServerName; optionType < OptionType.NumOfSlots; optionType++)
			{
				ApplyNativeOption(optionType, optionList[optionType]);
			}
		}

		/// <summary>
		/// Apply a single native option if its value has changed.
		/// </summary>
		public void ApplyNativeOption(OptionType optionType, object newValue)
		{
			MultiplayerOptionsProperty optionProperty = optionType.GetOptionProperty();
			object currentValue = TWConfig.GetCurrentServerValue(optionType);

			// Skip if value hasn't changed
			if (AreValuesEqual(currentValue, newValue, optionProperty.OptionValueType))
			{
				return;
			}

			// Apply the new value based on type
			SetOptionValue(optionType, newValue, optionProperty.OptionValueType);

			// Log the change
			Log($"Updated native option {optionType}: {currentValue} -> {newValue}", LogLevel.Debug);
		}

		/// <summary>
		/// Compare two values based on their type.
		/// </summary>
		private static bool AreValuesEqual(object current, object newValue, OptionValueType valueType)
		{
			return valueType switch
			{
				OptionValueType.Bool => (bool)current == (bool)newValue,
				OptionValueType.Integer or OptionValueType.Enum => (int)current == (int)newValue,
				OptionValueType.String => (string)current == (string)newValue,
				_ => throw new ArgumentOutOfRangeException(nameof(valueType))
			};
		}

		/// <summary>
		/// Set option value based on its type.
		/// </summary>
		private static void SetOptionValue(OptionType optionType, object value, OptionValueType valueType)
		{
			switch (valueType)
			{
				case OptionValueType.Bool:
					optionType.SetValue((bool)value);
					break;
				case OptionValueType.Integer:
				case OptionValueType.Enum:
					optionType.SetValue((int)value);
					break;
				case OptionValueType.String:
					optionType.SetValue((string)value);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(valueType));
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
			if (modOptions == null)
			{
				Log("Tried to apply empty mod options, skipping...", LogLevel.Warning);
				return;
			}
			UpdateConfigFromDeserialized(modOptions, modOptions.SyncConfig);
		}
	}
}