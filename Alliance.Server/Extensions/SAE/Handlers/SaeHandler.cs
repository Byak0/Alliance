using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using Alliance.Server.Extensions.SAE.Behaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE.Handlers
{
    public class SaeHandler
    {
        SaeBehavior saeBehavior => Mission.Current.GetMissionBehavior<SaeBehavior>();
        public SaeHandler() { }

        public bool OnSaeCreateMarkerMessageReceived(NetworkCommunicator peer, SaeCreateDynamicMarkerNetworkClientMessage message)
        {
            if (saeBehavior == null)
            {
                Debug.Print("Server: SaeBehavior is NULL !", 0, Debug.DebugColor.Red);
                return false;
            }

            Debug.Print("Server: Request to create a dynamic marker received !", 0, Debug.DebugColor.Purple);

            // Get from SaeBehavior the Dictionary that contain all basic map markers.
            Dictionary<MatrixFrame, bool> dico = saeBehavior.getDynamicMarkersDico();

            // From this list, take the ones that are near client request point.

            List<MatrixFrame> eligiblePositions = new List<MatrixFrame>();

            message.markersPosition.ForEach(e =>
            {
                MatrixFrame positionWhereSAEMarkerNeedToBe = FindNearestEntity(dico.Select(e => e.Key).ToList(), e);
                if (!positionWhereSAEMarkerNeedToBe.IsZero)
                {
                    eligiblePositions.Add(positionWhereSAEMarkerNeedToBe);
                }
            });

            saeBehavior.AddDynamicMarkerToDicoAndSendMessageToClient(eligiblePositions, peer.ControlledAgent.Team, peer);

            return true;
        }

        private MatrixFrame FindNearestEntity(List<MatrixFrame> fakeMarkersList, MatrixFrame positionToSearch)
        {
            Debug.Print("Server: position to search = " + positionToSearch.origin, 0, Debug.DebugColor.Purple);
            MatrixFrame nearestEntity = MatrixFrame.Zero;

            foreach (MatrixFrame fakeMarker in fakeMarkersList)
            {
                if (fakeMarker.origin.Distance(positionToSearch.origin) < nearestEntity.origin.Distance(positionToSearch.origin))
                {
                    nearestEntity = fakeMarker;
                }
            }

            Debug.Print("Server: Nearest is at coordonates  " + nearestEntity.origin, 0, Debug.DebugColor.Purple);
            return nearestEntity;
        }
    }
}
