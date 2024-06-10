using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer.AdminServerLog;
using TaleWorlds.Diamond;
using System;
using System.Text;
using System.Linq;
using static Alliance.Common.Utilities.Logger;


namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer
{
    /// <summary>
    /// TeamKillTrackerLog : used to transmit friendly fire data registered server side to the client
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class TeamKillTrackerLog : GameNetworkMessage
    {
        // Only thing we transmit is the list of team damage done by player
        public Dictionary<string, int> TeamDamageByPlayer { get; set; }

        public TeamKillTrackerLog()
        {
        }

        public TeamKillTrackerLog(Dictionary<string, int> teamDamageByPlayer)
        {
            TeamDamageByPlayer = teamDamageByPlayer;
        }

        protected override void OnWrite()
        {
            try
            {
                // Serializing the dictionary to a string before sending it
                StringBuilder packetToSend = new StringBuilder();
                foreach (KeyValuePair<string, int> playerTeamDamage in TeamDamageByPlayer)
                {
                    packetToSend.Append(playerTeamDamage.Key.Replace('~', ' ').Replace('|', ' ')); // We remove separator char from player names to avoid any deseriazlization problems
                    packetToSend.Append('~');
                    packetToSend.Append(playerTeamDamage.Value);
                    packetToSend.Append('|');
                }

                // Sending to client the serialized string
                WriteStringToPacket(packetToSend.ToString());
            }
            catch (Exception e)
            {
                Log($"Exception dans l'écriture de TeamKillTrackerLog : {e.Message}", LogLevel.Warning);
            }
        }

        protected override bool OnRead()
        {
            
            bool bufferReadValid = true;
            try
            {
                TeamDamageByPlayer = new Dictionary<string, int>();
                string packet = ReadStringFromPacket(ref bufferReadValid);

                // Deserializing the string toa dictionary on reception
                List<string> playerDamageList = packet.Split('|').ToList();
                foreach (string playerDamage in playerDamageList)
                {
                    if (!string.IsNullOrWhiteSpace(playerDamage))
                    {
                        TeamDamageByPlayer.Add(playerDamage.Split('~')[0], int.Parse(playerDamage.Split('~')[1]));
                    }
                }
            }
            catch (Exception e)
            {
                Log($"Exception dans la lecture de TeamKillTrackerLog : {e.Message}", LogLevel.Warning);
            }

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }
        protected override string OnGetLogFormat()
        {
            return "Serveur Network message log";
        }
    }
}
