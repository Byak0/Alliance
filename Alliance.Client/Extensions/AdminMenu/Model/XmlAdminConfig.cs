using Alliance.Common.Utilities;
using System;
using System.IO;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.AdminMenu.Model
{
	public class XmlAdminConfig
	{
		private static XmlAdminConfig _instance;
		private static readonly string CONFIG_PATH = PathHelper.GetAllianceDocumentFilePath("AdminConfig.xml");

		public bool CanSeeAllPlayersNames { get; set; } = true;

		private XmlAdminConfig() { }

		public static XmlAdminConfig GetInstance()
		{
			if (_instance == null)
			{
				_instance = LoadFromFile();
			}

			return _instance;
		}

		private static XmlAdminConfig LoadFromFile()
		{
			Log("Loading admin config from file: " + CONFIG_PATH);

			if (!File.Exists(CONFIG_PATH))
			{
				Log("Config file does not exist. Creating default config file.", LogLevel.Warning);
				XmlAdminConfig defaultConfig = new XmlAdminConfig();
				SaveToFile(defaultConfig);
				return defaultConfig;
			}

			try
			{
				System.Xml.Serialization.XmlSerializer serializer =
					new System.Xml.Serialization.XmlSerializer(typeof(XmlAdminConfig));

				using (FileStream stream = new FileStream(CONFIG_PATH, FileMode.Open))
				{
					return (XmlAdminConfig)serializer.Deserialize(stream);
				}
			}
			catch (Exception ex)
			{
				Log("Error loading admin config: " + ex.Message, LogLevel.Error);
				Log("Returning default config.", LogLevel.Error);
				return new XmlAdminConfig();
			}
		}

		private static void SaveToFile(XmlAdminConfig config)
		{
			try
			{
				string directory = Path.GetDirectoryName(CONFIG_PATH);
				if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
				{
					Directory.CreateDirectory(directory);
				}

				System.Xml.Serialization.XmlSerializer serializer =
					new System.Xml.Serialization.XmlSerializer(typeof(XmlAdminConfig));

				using (FileStream stream = new FileStream(CONFIG_PATH, FileMode.Create))
				{
					serializer.Serialize(stream, config);
				}

				Log("Admin config saved to file: " + CONFIG_PATH);
			}
			catch (Exception ex)
			{
				Log("Error saving admin config: " + ex.Message, LogLevel.Error);
			}
		}
	}
}