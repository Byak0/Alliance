using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromClient;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.TroopSpawner.Models
{
    /// <summary>
    /// Singleton model storing which players control which formations.
    /// Synchronized between server and clients.
    /// </summary>
    public class FormationControlModel
    {
        public struct PlayerTeamKey
        {
            public MissionPeer MissionPeer;
            public Team Team;

            public PlayerTeamKey(MissionPeer missionPeer, Team team)
            {
                MissionPeer = missionPeer;
                Team = team;
            }
        }

        private static readonly FormationControlModel instance = new();
        public static FormationControlModel Instance { get { return instance; } }

        public event Action FormationControlChanged;
        private readonly Dictionary<PlayerTeamKey, List<FormationClass>> playerFormationMapping = new();

        public FormationControlModel()
        {
        }

        public void Clear()
        {
            playerFormationMapping.Clear();
            Log($"Cleared formation control model", LogLevel.Debug);
        }

        /// <summary>
        /// Request to assign control of a formation to a player.
        /// </summary>
        public void RequestAssignControlToPlayer(MissionPeer missionPeer, FormationClass formationClass)
        {
            Log($"Request assign control of {formationClass} to {missionPeer.Name}", LogLevel.Debug);
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new FormationRequestControlMessage(missionPeer.GetNetworkPeer(), formationClass));
            GameNetwork.EndModuleEventAsClient();
        }

        /// <summary>
        /// Refresh player control over its formations. Useful when respawning.
        /// </summary>
        public void ReassignControlToAgent(Agent agent)
        {
            if (agent.MissionPeer == null) return;

            MissionPeer player = agent.MissionPeer;
            PlayerTeamKey key = new PlayerTeamKey(player, player.Team);

            if (playerFormationMapping.TryGetValue(key, out List<FormationClass> controlledFormations))
            {
                foreach (FormationClass controlledFormation in controlledFormations)
                {
                    agent.Team.AssignPlayerAsSergeantOfFormation(player, controlledFormation);
                }
            }
        }

        /// <summary>
        /// Give control of a formation to a player.
        /// </summary>
        /// <param name="sync">Set this to true if you want to synchronize with all clients</param>
        public void AssignControlToPlayer(MissionPeer missionPeer, FormationClass formationClass, bool sync = false)
        {
            PlayerTeamKey key = new PlayerTeamKey(missionPeer, missionPeer.Team);

            if (!playerFormationMapping.ContainsKey(key))
            {
                playerFormationMapping[key] = new List<FormationClass>();
            }

            if (!playerFormationMapping[key].Contains(formationClass))
            {
                if (sync)
                {
                    // Check if another player already control this formation
                    foreach (KeyValuePair<PlayerTeamKey, List<FormationClass>> kvp in playerFormationMapping)
                    {
                        if (kvp.Value.Contains(formationClass))
                        {
                            if (kvp.Key.Team == missionPeer.Team)
                            {
                                RemoveControlFromPlayer(kvp.Key.MissionPeer, formationClass, true);
                            }
                        }
                    }
                }

                playerFormationMapping[key].Add(formationClass);
                if (GameNetwork.IsServer) missionPeer.ControlledAgent?.Team.AssignPlayerAsSergeantOfFormation(key.MissionPeer, formationClass);
                FormationControlChanged?.Invoke();

                Log($"Assigned {key.MissionPeer.Name} control over formation {formationClass}", LogLevel.Debug);
            }

            if (sync)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new FormationControlMessage(key.MissionPeer.GetNetworkPeer(), formationClass));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        /// <summary>
        /// Remove control of a formation from a player.
        /// </summary>
        /// <param name="sync">Set this to true if you want to synchronize with all clients</param>
        public void RemoveControlFromPlayer(MissionPeer missionPeer, FormationClass formationClass, bool sync = false)
        {
            PlayerTeamKey key = new PlayerTeamKey(missionPeer, missionPeer.Team);

            if (playerFormationMapping.TryGetValue(key, out var controlledFormations))
            {
                if (controlledFormations.Contains(formationClass))
                {
                    controlledFormations.Remove(formationClass);
                    if (GameNetwork.IsServer) key.MissionPeer.ControlledFormation = null;
                    FormationControlChanged?.Invoke();
                }

                if (controlledFormations.Count == 0)
                {
                    playerFormationMapping.Remove(key);
                }

                Log($"Removed {key.MissionPeer.Name} control over formation {formationClass}", LogLevel.Debug);
            }

            if (sync)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new FormationControlMessage(key.MissionPeer.GetNetworkPeer(), formationClass, false));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        public void TransferControl(MissionPeer fromPeer, MissionPeer toPeer, FormationClass formationClass, bool sync = false)
        {
            RemoveControlFromPlayer(fromPeer, formationClass, sync);
            AssignControlToPlayer(toPeer, formationClass, sync);
        }

        public void SendMappingToClient(NetworkCommunicator peer)
        {
            Log($"Sending formation mapping info to {peer.UserName}. Commanders : {playerFormationMapping.Count}", LogLevel.Debug);

            foreach (KeyValuePair<PlayerTeamKey, List<FormationClass>> kvp in playerFormationMapping)
            {
                if (kvp.Key.MissionPeer != null)
                {
                    foreach (FormationClass formationClass in kvp.Value)
                    {
                        GameNetwork.BeginModuleEventAsServer(peer);
                        GameNetwork.WriteMessage(new FormationControlMessage(peer, formationClass));
                        GameNetwork.EndModuleEventAsServer();
                    }
                }
            }
        }

        public List<FormationClass> GetControlledFormations(MissionPeer missionPeer)
        {
            PlayerTeamKey key = new PlayerTeamKey(missionPeer, missionPeer.Team);

            if (playerFormationMapping.TryGetValue(key, out var controlledFormations))
            {
                return controlledFormations;
            }
            return new List<FormationClass>();
        }

        public List<string> GetAllControllersNameFromTeam(Team team)
        {
            List<string> controllers = new();

            foreach (KeyValuePair<PlayerTeamKey, List<FormationClass>> kvp in playerFormationMapping)
            {
                if (kvp.Key.Team == team)
                {
                    controllers.Add(kvp.Key.MissionPeer.Name);
                }
            }

            return controllers;
        }

        public List<MissionPeer> GetAllControllersFromTeam(Team team)
        {
            List<MissionPeer> controllers = new();

            foreach (KeyValuePair<PlayerTeamKey, List<FormationClass>> kvp in playerFormationMapping)
            {
                if (kvp.Key.Team == team)
                {
                    controllers.Add(kvp.Key.MissionPeer);
                }
            }

            return controllers;
        }

        public MissionPeer GetControllerOfFormation(FormationClass i, Team team)
        {
            foreach (KeyValuePair<PlayerTeamKey, List<FormationClass>> kvp in playerFormationMapping)
            {
                if (kvp.Key.Team == team && kvp.Value.Contains(i))
                {
                    return kvp.Key.MissionPeer;
                }
            }

            return null;
        }

        public MissionPeer GetControllerOfFormation(Formation formation)
        {
            foreach (KeyValuePair<PlayerTeamKey, List<FormationClass>> kvp in playerFormationMapping)
            {
                if (kvp.Key.Team == formation.Team && kvp.Value.Contains(formation.FormationIndex))
                {
                    return kvp.Key.MissionPeer;
                }
            }

            return null;
        }

        public bool IsPlayerControllingAgent(MissionPeer player, Agent followedAgent)
        {
            PlayerTeamKey key = new PlayerTeamKey(player, player.Team);
            bool isPlayerControllingAgent = false;
            if (player.Team == followedAgent.Team && playerFormationMapping.TryGetValue(key, out List<FormationClass> controlledForms))
            {
                if (controlledForms.Contains(followedAgent.Formation.FormationIndex))
                {
                    isPlayerControllingAgent = true;
                }
            }
            return isPlayerControllingAgent;
        }
    }
}
