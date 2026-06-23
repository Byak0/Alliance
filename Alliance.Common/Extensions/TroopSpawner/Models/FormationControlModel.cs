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
        private static readonly FormationControlModel instance = new();
        public static FormationControlModel Instance { get { return instance; } }

        public event Action<int, FormationClass, MissionPeer> FormationControlChanged;
		private readonly Dictionary<int, Dictionary<FormationClass, MissionPeer>> playerFormationMapping = new();

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
        public void RequestAssignControlToPlayer(MissionPeer missionPeer, int formationIndex)
        {
            Log($"Request assign control of formation {formationIndex} to {missionPeer.Name}", LogLevel.Debug);
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new FormationRequestControlMessage(missionPeer.GetNetworkPeer(), formationIndex));
            GameNetwork.EndModuleEventAsClient();
        }

        /// <summary>
        /// Refresh player control over its formations. Useful when respawning.
        /// </summary>
        public void ReassignControlToAgent(Agent agent)
        {
            if (agent.MissionPeer == null) return;

            if(playerFormationMapping.TryGetValue(agent.Team.TeamIndex, out var formationMapping))
			{
				foreach (KeyValuePair<FormationClass, MissionPeer> kvp in formationMapping)
				{
					if (kvp.Value == agent.MissionPeer)
					{
						agent.Team.AssignPlayerAsSergeantOfFormation(agent.MissionPeer, kvp.Key);
					}
				}
			}
        }

        /// <summary>
        /// Give control of a formation to a player.
        /// </summary>
        /// <param name="sync">Set this to true if you want to synchronize with all clients</param>
        public void AssignControlToPlayer(MissionPeer missionPeer, int teamIndex, FormationClass formationClass, bool sync = false)
        {
            if (!playerFormationMapping.TryGetValue(teamIndex, out var formationMapping))
            {
                playerFormationMapping[teamIndex] = new Dictionary<FormationClass, MissionPeer>();
            }
			
            // Remove control from any other player controlling this formation
            if (formationMapping.TryGetValue(formationClass, out MissionPeer currentController))
			{
				if (currentController != missionPeer)
				{
					RemoveControlFromPlayer(currentController, teamIndex, formationClass, true);
				}
			}

			// Assign control to the new player
			formationMapping[formationClass] = missionPeer;
            if (GameNetwork.IsServer) missionPeer.ControlledAgent?.Team.AssignPlayerAsSergeantOfFormation(missionPeer, formationClass);
            FormationControlChanged?.Invoke(teamIndex, formationClass, missionPeer);
            Log($"Assigned {missionPeer.Name} control over team {teamIndex} formation {formationClass}", LogLevel.Debug);

            if (sync)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new FormationControlMessage(missionPeer.GetNetworkPeer(), teamIndex, formationClass));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        /// <summary>
        /// Remove control of a formation from a player.
        /// </summary>
        /// <param name="sync">Set this to true if you want to synchronize with all clients</param>
        public void RemoveControlFromPlayer(MissionPeer missionPeer, int teamIndex, FormationClass formationClass, bool sync = false)
        {
            if (playerFormationMapping.TryGetValue(teamIndex, out var formationMapping))
            {
                if(formationMapping.TryGetValue(formationClass, out MissionPeer controller))
                {
					if (controller == missionPeer)
					{
                        formationMapping[formationClass] = null;
                        if(GameNetwork.IsServer) missionPeer.ControlledFormation = null;
                        FormationControlChanged?.Invoke(teamIndex, formationClass, null);
					}
				}

                Log($"Removed {missionPeer.Name} control over team {teamIndex} formation {formationClass}", LogLevel.Debug);
            }

            if (sync)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new FormationControlMessage(missionPeer.GetNetworkPeer(), teamIndex, formationClass, false));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        public void RemoveAllControlFromPlayer(MissionPeer missionPeer, bool sync = false)
		{
            foreach(KeyValuePair<int, Dictionary<FormationClass, MissionPeer>> teamMap in playerFormationMapping)
			{
				foreach (KeyValuePair<FormationClass, MissionPeer> formationMap in teamMap.Value)
				{
					if (formationMap.Value == missionPeer)
					{
						RemoveControlFromPlayer(missionPeer, teamMap.Key, formationMap.Key, sync);
					}
				}
			}
		}

		public void TransferControl(MissionPeer fromPeer, MissionPeer toPeer, FormationClass formationClass, bool sync = false)
        {
            RemoveControlFromPlayer(fromPeer, toPeer.Team.TeamIndex, formationClass, sync);
            AssignControlToPlayer(toPeer, toPeer.Team.TeamIndex, formationClass, sync);
        }

        public void SendMappingToClient(NetworkCommunicator peer)
        {
            Log($"Sending formation mapping info to {peer.UserName}. Commanders : {playerFormationMapping.Count}", LogLevel.Debug);

			foreach (KeyValuePair<int, Dictionary<FormationClass, MissionPeer>> teamMap in playerFormationMapping)
			{
				foreach (KeyValuePair<FormationClass, MissionPeer> formationMap in teamMap.Value)
				{
					if (formationMap.Value != null)
					{
						GameNetwork.BeginModuleEventAsServer(peer);
						GameNetwork.WriteMessage(new FormationControlMessage(formationMap.Value.GetNetworkPeer(), teamMap.Key, formationMap.Key));
						GameNetwork.EndModuleEventAsServer();
					}
				}
			}
        }

        public List<FormationClass> GetControlledFormations(MissionPeer missionPeer)
        {            
            if(missionPeer.Team == null) return new List<FormationClass>();

			playerFormationMapping.TryGetValue(missionPeer.Team.TeamIndex, out var formationMapping);

            List<FormationClass> controlledFormations = new();
			foreach (KeyValuePair<FormationClass, MissionPeer> kvp in formationMapping)
			{
				if (kvp.Value == missionPeer)
				{
					controlledFormations.Add(kvp.Key);
				}
			}

			return controlledFormations;
        }

        public List<MissionPeer> GetAllControllersFromTeam(Team team)
        {
            List<MissionPeer> controllers = new();

            if(playerFormationMapping.TryGetValue(team.TeamIndex, out var formationMapping))
			{
				foreach (KeyValuePair<FormationClass, MissionPeer> kvp in formationMapping)
				{
					if (kvp.Value != null && !controllers.Contains(kvp.Value))
					{
						controllers.Add(kvp.Value);
					}
				}
			}
            
            return controllers;
        }

        public MissionPeer GetControllerOfFormation(FormationClass i, Team team)
        {
            if(playerFormationMapping.TryGetValue(team.TeamIndex, out var formationMapping))
            {
                if (formationMapping.TryGetValue(i, out var controller))
                {
                    return controller;
                }
            }

            return null;
        }

        public MissionPeer GetControllerOfFormation(Formation formation)
        {
			if (playerFormationMapping.TryGetValue(formation.Team.TeamIndex, out var formationMapping))
			{
				if (formationMapping.TryGetValue(formation.FormationIndex, out var controller))
				{
					return controller;
				}
			}

			return null;
		}

        public bool IsPlayerControllingAgent(MissionPeer player, Agent followedAgent)
        {
            return GetControllerOfFormation(followedAgent.Formation) == player;
        }
    }
}
