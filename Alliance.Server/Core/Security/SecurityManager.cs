using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.Models;
using Alliance.Common.Utilities;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Security
{
    /// <summary>
    /// Server-side class used to update players roles and access level.
    /// </summary>
    public class SecurityManager
    {
        public static DateTime lastRead = DateTime.MinValue;

        public static void AddBan(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Banned), player.Id);
        }

        public static void RemoveBan(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Banned), player.Id, true);
        }

        public static void AddAdmin(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Admins), player.Id);
        }

        public static void RemoveAdmin(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Admins), player.Id, true);
        }

        public static void AddModerator(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Moderators), player.Id);
        }

        public static void RemoveModerator(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Moderators), player.Id, true);
        }

        public static void AddDev(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Devs), player.Id);
        }

        public static void RemoveDev(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Devs), player.Id, true);
        }

        public static void AddCommander(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Commanders), player.Id);
        }

        public static void RemoveCommander(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Commanders), player.Id, true);
        }

        public static void AddOfficer(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Officers), player.Id);
        }

        public static void RemoveOfficer(VirtualPlayer player)
        {
            UpdateRole(nameof(DefaultRoles.Officers), player.Id, true);
        }

        private static void UpdateRole(string roleName, PlayerId playerId, bool remove = false)
        {
            try
            {
                // Update role in memory
                int roleIndex = GetRoleIndex(roleName);
                RoleManager.Instance.UpdatePlayersRoles(roleIndex, playerId, remove);

                // Sync roles
                RoleManager.Instance.SyncPlayersRoles(roleIndex, playerId, remove);

                // Save roles to file
                SerializeHelper.SaveClassToFile(SubModule.RolesFilePath, Roles.Instance);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to update role {roleName} for player {playerId} :", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        private static int GetRoleIndex(string roleName)
        {
            FieldInfo fi = typeof(DefaultRoles).GetField(roleName);
            return RoleManager.Instance.RolesFields.FirstOrDefault(x => x.Value == fi).Key;
        }
    }
}
