using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromServer;
using Alliance.Server.Extensions.SAE.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.SAE.Handlers
{
    /// <summary>
    /// This handler will handle message send by a client to the server
    /// So this code will only be executed by server
    /// </summary>
    public class SaeDeleteMarkerHandler
    {
        private readonly SaeBehavior SaeBehavior;
        public SaeDeleteMarkerHandler()
        {
            SaeBehavior = Mission.Current.GetMissionBehavior<SaeBehavior>();
        }

        public bool OnSaeDeleteForAllClientMessageReceived(NetworkCommunicator peer, SaeDeleteMarkerForAllNetworkClientMessage message)
        {
            Debug.Print("Server: Deleting " + message.ListOfMarkersId.Count + " markers", 0, Debug.DebugColor.Red);

            //Remove markers from the server list
            SaeBehavior.DeleteMarkersToTeam(message.ListOfMarkersId, peer.ControlledAgent.Team);

            SendMarkerListToDeleteToAllPeersOfSameTeam(peer, message.ListOfMarkersId);

            return true;
        }

        public bool OnSaeDeleteAllForAllClientMessageReceived(NetworkCommunicator peer, SaeDeleteAllMarkersForAllNetworkClientMessage message)
        {
            Log($"Server: Deleting all {peer.ControlledAgent.Team} markers", LogLevel.Information);
            List<int> listMarkers = SaeBehavior.SaeMarkerTeams
                .Find(saeList => saeList.Team.Side == peer.ControlledAgent.Team.Side)
                .SaeMarkers
                .Select(markers => markers.Id)
                .ToList();
            SendMarkerListToDeleteToAllPeersOfSameTeam(peer, listMarkers);
            SaeBehavior.DeleteAllMarkersToTeam(peer.ControlledAgent.Team);

            return true;
        }

        public void SendMarkerListToDeleteToAllPeersOfSameTeam(NetworkCommunicator peer, List<int> markerlist)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SaeDeleteMarkerNetworkServerMessage(markerlist));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, peer);
        }
    }
}
