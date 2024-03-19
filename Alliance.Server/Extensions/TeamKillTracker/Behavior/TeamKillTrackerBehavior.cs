using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Server.Extensions.AIBehavior.TacticComponents;
using Alliance.Server.Extensions.AIBehavior.TeamAIComponents;
using Alliance.Server.Extensions.AIBehavior.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Network;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.TeamKillTracker.Behavior
{
    /// <summary>
    /// Centralized behavior for registering team damage
    /// to prevent conflicts.
    /// </summary>
    public class TeamKillTrackerBehavior : MissionNetwork
    {

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

        // Dictionary with the name of the player doing the damage as key and the amount of team damage registered so far as value
        private Dictionary<string, int> _teamDamageByPlayer = new Dictionary<string, int>();

        public override void OnBehaviorInitialize()
        {
            _teamDamageByPlayer = new Dictionary<string, int>();
        }

        public override void AfterStart()
        {
        }

        public override void OnRemoveBehavior()
        {
        }

        /// <summary>
        /// OnAgentHit check if target is on the same team, if it is, register the damage in the _teamDamageByPlayer dictionay, send the dictionnary and an admin message to the client
        /// </summary>
        /// <param name="affectedAgent"></param>
        /// <param name="affectorAgent"></param>
        /// <param name="affectorWeapon"></param>
        /// <param name="blow"></param>
        /// <param name="attackCollisionData"></param>
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            try
            {

                // If the damage done is friendly
                // Team might be null for Agent without team (ie : horses), in this case the damage done is ignored
                if (affectorAgent.Team != null
                    && affectedAgent.Team != null
                    && affectorAgent.IsPlayerControlled == true
                    && affectorAgent.Team.IsFriendOf(affectedAgent.Team))
                {

                    // Check the dictionary for friendly damage already done, if the player name is already known, we add the inflicted damage to the already logged damage in the dictionary, otherwise we add an entry for the player
                    if (_teamDamageByPlayer.ContainsKey(affectorAgent.Name))
                        _teamDamageByPlayer[affectorAgent.Name] += blow.InflictedDamage;
                    else
                        _teamDamageByPlayer.Add(affectorAgent.Name, blow.InflictedDamage);

                    // Sending the dictionay with all the FF to the client via the TeamKillTrackerLog
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new TeamKillTrackerLog(_teamDamageByPlayer));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients, null);        
                    // Sending a message in the admin console notifying the FF
                    GameNetwork.BeginBroadcastModuleEvent(); // Note : one broadcastmoduleevent seems to only be able to write 1 message, that's why it's duplicated here
                    GameNetwork.WriteMessage(new AdminServerLog($"{affectorAgent.Name} touche son allie {affectedAgent.Name} pour {blow.InflictedDamage} ({_teamDamageByPlayer[affectorAgent.Name]})", AdminServerLog.ColorList.Warning));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients, null);
                }
            }
            catch (Exception e)
            {
                Log($"Exception dans le TeamKillTrackerBehavior : {e.Message}", LogLevel.Warning);
            }

        }


    }
}
