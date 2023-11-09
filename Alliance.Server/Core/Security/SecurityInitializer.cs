using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.Models;
using Alliance.Common.Utilities;
using System;
using System.IO;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Security
{
    /// <summary>
    /// Initializer for players roles. Keep watch over roles file to detect real-time changes.
    /// </summary>
    public class SecurityInitializer
    {
        public static DateTime lastRead = DateTime.MinValue;

        /// <summary>
        /// Initialize the players roles and start watching the roles file.
        /// </summary>
        public static void Init()
        {
            Roles.Instance.Init();
            Roles.Instance = SerializeHelper.LoadClassFromFile(SubModule.RolesFilePath, Roles.Instance);

            // Watch changes to the roles file
            SerializeHelper.CreateFileWatcher(SubModule.RolesFilePath, OnRolesFileChanged);
        }

        /// <summary>
        /// Called when changes are made manually to the roles file.
        /// </summary>
        public static void OnRolesFileChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                DateTime lastWriteTime = File.GetLastWriteTime(SubModule.RolesFilePath);
                if (lastWriteTime.Ticks - lastRead.Ticks > 10000000) // Prevent changing config too fast
                {
                    RoleManager.Instance.UpdatePlayersFromDeserialized(SerializeHelper.LoadClassFromFile(SubModule.RolesFilePath, Roles.Instance), true);
                    lastRead = lastWriteTime;
                }
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to update roles :", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
