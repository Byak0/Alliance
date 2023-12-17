using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Security.Models;
using Alliance.Common.Core.Security.NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Security
{
    /// <summary>
    /// Manage roles, update values and ensure synchronization with clients
    /// </summary>  
    public class RoleManager
    {
        /// <summary>
        /// List of fields from Roles class. Used to identify fields with their index and reduce network load in synchronization.
        /// </summary>
        public readonly Dictionary<int, FieldInfo> RolesFields = new Dictionary<int, FieldInfo>();

        private static readonly RoleManager instance = new RoleManager();

        public static RoleManager Instance
        {
            get
            {
                return instance;
            }
        }

        private RoleManager()
        {
            var rolesFields = typeof(DefaultRoles).GetFields(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < rolesFields.Length; i++)
            {
                RolesFields.Add(i, rolesFields[i]);
            }
        }

        /// <summary>
        /// Returns all the players presents in only one of the two lists.
        /// List1 = [A,B,C]
        /// List2 = [B,C,D]
        /// Return value = [A,D]
        /// </summary>
        public static List<Player> SymmetricPlayerExceptWith(List<Player> list1, List<Player> list2)
        {
            List<Player> tempList = new List<Player>(list1);
            List<Player> newList = new List<Player>();

            newList.AddRange(list2.Except(tempList));
            tempList.RemoveAll(list2.Contains);
            newList.AddRange(tempList);

            return newList;
        }

        /// <summary>
        /// Update special players from a deserialized version of the class.
        /// Compare both versions and update only the difference.
        /// </summary>
        public void UpdatePlayersFromDeserialized(Roles deserialized, bool synchronize = false)
        {
            foreach (KeyValuePair<int, FieldInfo> role in RolesFields)
            {
                List<Player> currentList = (List<Player>)role.Value.GetValue(Roles.Instance);
                List<Player> deserializedList = (List<Player>)role.Value.GetValue(deserialized);
                List<Player> difference = SymmetricPlayerExceptWith(currentList, deserializedList);
                foreach (Player player in difference)
                {
                    bool removed = !deserializedList.Contains(player);
                    UpdatePlayersRoles(role.Key, player, removed);
                    if (synchronize) SyncPlayersRoles(role.Key, player.Id, removed);
                }
            }
        }

        public void SendPlayersRolesToPeer(NetworkCommunicator networkPeer)
        {
            foreach (KeyValuePair<int, FieldInfo> role in RolesFields)
            {
                List<Player> currentList = (List<Player>)role.Value.GetValue(Roles.Instance);
                foreach (Player player in currentList)
                {
                    GameNetwork.BeginModuleEventAsServer(networkPeer);
                    GameNetwork.WriteMessage(new UpdateRole(role.Key, player.Id));
                    GameNetwork.EndModuleEventAsServer();
                }
            }
        }

        public void SendPlayersRolesToAllPeers()
        {
            foreach (KeyValuePair<int, FieldInfo> role in RolesFields)
            {
                List<Player> currentList = (List<Player>)role.Value.GetValue(Roles.Instance);
                foreach (Player player in currentList)
                {
                    SyncPlayersRoles(role.Key, player.Id);
                }
            }
        }

        public void UpdatePlayersRoles(int fieldIndex, PlayerId playerId, bool remove = false)
        {
            string playerName = RolesExtension.GetPlayerName(playerId);
            Player player = new Player(playerName, playerId);
            UpdatePlayersRoles(fieldIndex, player, remove);
        }

        public void UpdatePlayersRoles(int fieldIndex, Player player, bool remove = false)
        {
            List<Player> oldList = (List<Player>)RolesFields[fieldIndex].GetValue(Roles.Instance);
            if (remove) oldList.Remove(player);
            else oldList.Add(player);

            Log("Alliance Role Manager - " + player.Name + " (" + player.Id + ")" + (remove ? " removed from " : " added to ") + RolesFields[fieldIndex].Name + ".", LogLevel.Debug);
        }

        public void SyncPlayersRoles(int fieldIndex, PlayerId player, bool remove = false)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new UpdateRole(fieldIndex, player, remove));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }
    }
}
