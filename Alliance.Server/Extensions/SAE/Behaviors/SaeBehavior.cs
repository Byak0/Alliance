using Alliance.Common.Extensions.SAE.Models;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromServer;
using Alliance.Server.Extensions.SAE.Handlers;
using Alliance.Server.Extensions.SAE.Models;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.SAE.Behaviors
{
    public class SaeBehavior : MissionNetwork, IMissionBehavior
    {
        /// <summary>
        /// Represent the nex available ID. Should be reset for each mission.
        /// </summary>
        private int nextAvailableId = 0;

        /// <summary>
        /// Contain the list of all current Teams and for each, the list of all markers.
        /// </summary>
        private List<SaeMarkerTeam> saeMarkerTeams;

        public List<SaeMarkerTeam> SaeMarkerTeams => saeMarkerTeams;

        Dictionary<MatrixFrame, bool> saeDynamicMapMarkersDico;

        public SaeBehavior() : base() { }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
        }

        public override void AfterStart()
        {
            base.AfterStart();

            Debug.Print("SaeBehavior Initialized", 0, Debug.DebugColor.Blue);

            List<Team> teams = Mission.Current.Teams;

            saeMarkerTeams = new List<SaeMarkerTeam>();
            teams.ForEach(team =>
            {
                saeMarkerTeams.Add(new SaeMarkerTeam(team));
            });

            //Init dynamicMarkersDico
            List<GameEntity> gameEntities = new();
            Mission.Current.Scene.GetEntities(ref gameEntities);
            saeDynamicMapMarkersDico = new Dictionary<MatrixFrame, bool>();
            gameEntities.Where(entity => entity.HasTag(SaeCommonConstants.FDC_QUICK_PLACEMENT_POS_TAG_NAME)).ToList()
                .ForEach(entity =>
                    saeDynamicMapMarkersDico.Add(
                        entity.GetGlobalFrame(),
                        false
                    )
                 );

            MultiplayerRoundController roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
            if (roundController != null)
            {
                roundController.OnPreparationEnded += ResetSAE;
                roundController.OnPostRoundEnded += ClearTeams;
            }
        }

        public override void OnEndMissionInternal()
        {
            ClearTeams();
            base.OnEndMissionInternal();
        }

        public override void OnMissionStateFinalized()
        {
            MultiplayerRoundController roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
            if (roundController != null)
            {
                roundController.OnPreparationEnded -= ResetSAE;
                roundController.OnPostRoundEnded -= ClearTeams;
            }
            base.OnMissionStateFinalized();
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, TaleWorlds.Core.AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent.MissionPeer != null && affectedAgent.Formation != null)
            {
                MissionPeer temp = affectedAgent.MissionPeer;
                affectedAgent.MissionPeer = null;
                affectedAgent.Formation.OnUndetachableNonPlayerUnitRemoved(affectedAgent);
                affectedAgent.MissionPeer = temp;
            }
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }

        private void ClearTeams()
        {
            //Deactivate all markers
            saeMarkerTeams.ForEach(e => DeleteMarkersToTeam(e.SaeMarkers.Select(e => e.Id).ToList(), e.Team));
        }

        public Dictionary<MatrixFrame, bool> getDynamicMarkersDico()
        {
            return saeDynamicMapMarkersDico;
        }

        public void AddDynamicMarkerToDicoAndSendMessageToClient(List<MatrixFrame> positionOfFakeArrow, Team team, NetworkCommunicator peer)
        {
            List<MatrixFrame> eligibleToCreation = new();

            positionOfFakeArrow.ForEach(position =>
            {
                bool isFound = saeDynamicMapMarkersDico.TryGetValue(position, out var valueOfTheKey);
                Log("Server: Did we found the marker to create ? => " + isFound, LogLevel.Debug);

                //If markerId exist and this marker has not already been created
                if (isFound && !saeDynamicMapMarkersDico[position])
                {
                    Log("Server: Position found and no markers have been created yet !", LogLevel.Debug);
                    //Marker will now be created
                    saeDynamicMapMarkersDico[position] = true;

                    eligibleToCreation.Add(position);
                }
            });

            if (eligibleToCreation.Count > 0)
            {
                Log("Server: Spawning a strategic area", LogLevel.Debug);
                List<SaeMarkerServerEntity> markerlist = AddMarkersToTeam(eligibleToCreation, peer.ControlledAgent.Team);

                markerlist.ForEach(m => SaeCreateMarkerHandler.InitStrategicAreaLogic(peer, m.StrategicArcherPointEntity));

                SaeCreateMarkerHandler.SendMarkersListToAllPeersOfSameTeam(peer, SaeCreateMarkerHandler.ConvertServerEntityToIdAndPos(markerlist));
            }
        }

        private void ResetSAE()
        {
            ClearTeams();
        }

        public void SendMarkersListToPeer(NetworkCommunicator networkPeer, List<SaeMarkerWithIdAndPos> markersId)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SaeCreateMarkersNetworkServerMessage(markersId));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);
        }

        /// <summary>
        /// Return the list of saeMarkers of a specific team.
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public List<SaeMarkerServerEntity> GetMarkerListDependingOnTeam(int side)
        {
            return saeMarkerTeams
                .Where(markerTeam => (int)markerTeam.Team.Side == side)
                .SelectMany(markerTeam => markerTeam.SaeMarkers)
                .ToList();
        }

        public List<SaeMarkerServerEntity> AddMarkersToTeam(List<MatrixFrame> makerPositionList, Team team)
        {
            List<SaeMarkerServerEntity> newSaeMarkers = new();
            SaeMarkerTeam markerTeam = saeMarkerTeams.Find(saeList => saeList.Team.Side == team.Side);

            makerPositionList.ForEach(
                markerPosition =>
                {
                    //Create marker
                    SaeMarkerServerEntity marker = new(markerPosition, team);

                    //Add marker to the returnedList
                    newSaeMarkers.Add(marker);

                    //Add it to the correct team list
                    markerTeam.SaeMarkers.Add(marker);
                });

            return newSaeMarkers;
        }

        public void DeleteAllMarkersToTeam(Team team)
        {
            List<GameEntity> gameEntities = new();
            Mission.Current.Scene.GetEntities(ref gameEntities);

            // Remove all strategic areas from Team AI
            team.TeamAI.RemoveAllStrategicAreas();

            SaeMarkerTeam markerTeam = saeMarkerTeams.Find(saeList => saeList.Team.Side == team.Side);

            //Retreive the list of elements that need to be deleted
            List<SaeMarkerServerEntity> gameEntitiesToDelete = markerTeam.SaeMarkers.ToList();

            gameEntitiesToDelete.ForEach(markerWithScriptToDelete =>
            {
                //Check if these markers are in Dico list
                //If yes, set them to false
                SetFalseToDicoKeyIfExist(markerWithScriptToDelete);

                StrategicArea strategicArea = markerWithScriptToDelete.StrategicArcherPointEntity.GetFirstScriptOfType<StrategicArea>();
                Log("strategicArea = " + strategicArea, LogLevel.Debug);
                if (strategicArea != null)
                {
                    strategicArea.SetDisabled(true);
                }
                markerWithScriptToDelete.StrategicArcherPointEntity.SetVisibilityExcludeParents(true);
            });

            //Remove all markers from the list of IDs
            Log("Before suppression = " + markerTeam.SaeMarkers.Count, LogLevel.Debug);

            markerTeam.SaeMarkers.Clear();

            Log("After suppression = " + markerTeam.SaeMarkers.Count, LogLevel.Debug);
        }

        public void DeleteMarkersToTeam(List<int> markersToDelete, Team team)
        {
            List<GameEntity> gameEntities = new();
            Mission.Current.Scene.GetEntities(ref gameEntities);

            SaeMarkerTeam markerTeam = saeMarkerTeams.Find(saeList => saeList.Team.Side == team.Side);

            //Retreive the list of elements that need to be deleted
            List<SaeMarkerServerEntity> gameEntitiesToDelete = markerTeam.SaeMarkers.Where(e => markersToDelete.Contains(e.Id)).ToList();

            gameEntitiesToDelete.ForEach(markerWithScriptToDelete =>
            {
                //Check if these markers are in Dico list
                //If yes, set them to false
                SetFalseToDicoKeyIfExist(markerWithScriptToDelete);

                StrategicArea strategicArea = markerWithScriptToDelete.StrategicArcherPointEntity.GetFirstScriptOfType<StrategicArea>();
                Log("strategicArea = " + strategicArea, LogLevel.Debug);
                if (strategicArea != null)
                {
                    markerTeam.Team.TeamAI.RemoveStrategicArea(strategicArea);
                    strategicArea.SetDisabled(true);
                }
                markerWithScriptToDelete.StrategicArcherPointEntity.SetVisibilityExcludeParents(true);
            });

            //Remove all markers from the list of IDs
            Log("Before suppression = " + markerTeam.SaeMarkers.Count, LogLevel.Debug);

            markersToDelete.ForEach(
                markerToDelete =>
                {
                    markerTeam.SaeMarkers.RemoveAll(markers => markers.Id == markerToDelete);
                }
            );

            Log("After suppression = " + markerTeam.SaeMarkers.Count, LogLevel.Debug);
        }

        private void SetFalseToDicoKeyIfExist(SaeMarkerServerEntity markerWithScriptToDelete)
        {
            if (saeDynamicMapMarkersDico.TryGetValue(markerWithScriptToDelete.StrategicArcherPointEntity.GetGlobalFrame(), out bool marker))
            {
                saeDynamicMapMarkersDico[markerWithScriptToDelete.StrategicArcherPointEntity.GetGlobalFrame()] = false;
            }
        }

        public int GetNextAvailableId()
        {
            return nextAvailableId++;
        }

        private class TacticSergeantMPBotTactic2 : TacticComponent
        {
            public TacticSergeantMPBotTactic2(Team team)
                : base(team)
            {
            }

            protected override void TickOccasionally()
            {

                foreach (Formation item in FormationsIncludingEmpty)
                {
                    if (item.CountOfUnits > 0)
                    {
                        item.AI.ResetBehaviorWeights();
                    }
                }
            }
        }

        private class TeamAIGeneral2 : TeamAIComponent
        {

            public TeamAIGeneral2(Mission currentMission, Team currentTeam, float thinkTimerTime = 10f, float applyTimerTime = 1f)
                : base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
            {
            }

            public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
            {
            }

            private void UpdateVariables()
            {
                TeamQuerySystem querySystem = Team.QuerySystem;
                Vec2 averagePosition = querySystem.AveragePosition;
                foreach (Agent agent in Mission.Agents)
                {
                    if (!agent.IsMount && agent.Team.IsValid && agent.Team.IsEnemyOf(Team))
                    {
                        float num = agent.Position.DistanceSquared(new Vec3(averagePosition.x, averagePosition.y));
                    }
                }
            }
        }

    }
}
