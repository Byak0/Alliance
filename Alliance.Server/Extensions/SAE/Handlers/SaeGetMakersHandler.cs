using Alliance.Common.Extensions.SAE.Models;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromServer;
using Alliance.Server.Extensions.SAE.Behaviors;
using Alliance.Server.Extensions.SAE.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE.Handlers
{
    /// <summary>
    /// This handler will handle message send by a client to the server
    /// So this code will only be executed by server
    /// </summary>
    public class SaeGetMarkersHandler
    {
        public SaeGetMarkersHandler() { }

        public bool OnSaeMessageReceived(NetworkCommunicator peer, SaeGetMarkersNetworkClientMessage message)
        {
            SaeBehavior behavior = Mission.Current.GetMissionBehavior<SaeBehavior>();

            if (behavior != null)
            {
                //Send to peer a message that contain the list of markers of his team
                List<SaeMarkerServerEntity> markers = behavior.GetMarkerListDependingOnTeam(message.Side);
                List<SaeMarkerWithIdAndPos> markersId = markers.Select(m => new SaeMarkerWithIdAndPos(m.Id, m.StrategicArcherPointEntity.GetGlobalFrame())).ToList();

                SendMarkersListToPeer(peer, markersId);

            }
            else
            {
                Debug.Print("SAE behavior is NULL !", 0, Debug.DebugColor.Red);
            }

            return true;
        }

        public void SendMarkersListToPeer(NetworkCommunicator networkPeer, List<SaeMarkerWithIdAndPos> markersId)
        {
            if (markersId.Count > 0)
            {
                Debug.Print("ENVOI DE LA LIST DES MARQUEUR A CREER COTE CLIENT !", 0, Debug.DebugColor.Red);

                int groupSize = 10;
                int totalElements = markersId.Count;

                for (int i = 0; i < totalElements; i += groupSize)
                {
                    int startIndex = i;
                    int endIndex = Math.Min(i + groupSize, totalElements);

                    List<SaeMarkerWithIdAndPos> group = markersId.GetRange(startIndex, endIndex - startIndex);

                    GameNetwork.BeginModuleEventAsServer(networkPeer);
                    GameNetwork.WriteMessage(new SaeCreateMarkersNetworkServerMessage(group));
                    GameNetwork.EndModuleEventAsServer();
                }
            }
        }
    }
}
