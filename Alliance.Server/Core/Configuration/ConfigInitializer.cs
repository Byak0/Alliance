using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Utilities;
using System;
using System.IO;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Configuration
{
    /// <summary>
    /// Initializer for configuration. Keep watch over config file to detect real-time changes.
    /// </summary>
    public class ConfigInitializer
    {
        public static DateTime lastRead = DateTime.MinValue;
        private static FileSystemWatcher _configWatcher;

        /// <summary>
        /// Initialize the player config and start watching the config file.
        /// </summary>
        public static void Init()
        {
            Config.Instance = SerializeHelper.LoadClassFromFile(SubModule.ConfigFilePath, Config.Instance);

            // Watch changes to the config file
            _configWatcher = SerializeHelper.CreateFileWatcher(SubModule.ConfigFilePath, OnConfigFileChanged);
        }

        /// <summary>
        /// Called when changes are made manually to the config file.
        /// </summary>
        private static void OnConfigFileChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                DateTime lastWriteTime = File.GetLastWriteTime(SubModule.ConfigFilePath);
                if (lastWriteTime.Ticks - lastRead.Ticks > 10000000) // Prevent changing config too fast
                {
                    ConfigManager.Instance.UpdateConfigFromDeserialized(SerializeHelper.LoadClassFromFile(SubModule.ConfigFilePath, Config.Instance), Config.Instance.SyncConfig);
                    lastRead = lastWriteTime;
                }
            }
            catch (Exception ex)
            {
                Log("Alliance - Failed to update config :", LogLevel.Error);
                Log(ex.Message, LogLevel.Error);
            }
        }
    }
}
