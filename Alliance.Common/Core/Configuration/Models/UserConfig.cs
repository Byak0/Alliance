using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Utilities;
using System;
using System.IO;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Configuration.Models
{
	public class UserConfig
	{
		private static UserConfig _instance;
		private static readonly string CONFIG_PATH = PathHelper.GetAllianceDocumentFilePath("UserConfig.xml");

		public string PreferredLanguage { get; set; } = LocalizationHelper.GetCurrentLanguage();
		public bool CanSeeAllPlayersNames { get; set; } = false;

		private UserConfig()
		{
			LocalizationHelper.GetCurrentLanguage();
		}

		public static UserConfig Instance
		{
			get
			{
				_instance ??= LoadFromFile();

				return _instance;
			}
		}

		public void Save()
		{
			SaveToFile(this);
		}

		private static UserConfig LoadFromFile()
		{
			Log("Loading user config from file: " + CONFIG_PATH);

			if (!File.Exists(CONFIG_PATH))
			{
				Log("Config file does not exist. Creating default config file.", LogLevel.Warning);
				UserConfig defaultConfig = new UserConfig();
				SaveToFile(defaultConfig);
				return defaultConfig;
			}

			try
			{
				System.Xml.Serialization.XmlSerializer serializer =
					new System.Xml.Serialization.XmlSerializer(typeof(UserConfig));

				using (FileStream stream = new FileStream(CONFIG_PATH, FileMode.Open))
				{
					return (UserConfig)serializer.Deserialize(stream);
				}
			}
			catch (Exception ex)
			{
				Log("Error loading user config: " + ex.Message, LogLevel.Error);
				Log("Returning default config.", LogLevel.Error);
				return new UserConfig();
			}
		}

		private static void SaveToFile(UserConfig config)
		{
			try
			{
				string directory = Path.GetDirectoryName(CONFIG_PATH);
				if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				System.Xml.Serialization.XmlSerializer serializer =
					new System.Xml.Serialization.XmlSerializer(typeof(UserConfig));

				using (FileStream stream = new FileStream(CONFIG_PATH, FileMode.Create))
				{
					serializer.Serialize(stream, config);
				}

				Log("User config saved to file: " + CONFIG_PATH);
			}
			catch (Exception ex)
			{
				Log("Error saving user config: " + ex.Message, LogLevel.Error);
			}
		}
	}
}