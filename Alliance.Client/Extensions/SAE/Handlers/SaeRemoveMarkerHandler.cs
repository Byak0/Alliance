using Alliance.Client.Extensions.SAE.Behaviors;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.SAE.Handlers
{
    public class SaeRemoveMarkerHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SaeDeleteMarkerNetworkServerMessage>(OnRemoveMarkersMessageReceived);
        }

        public void OnRemoveMarkersMessageReceived(SaeDeleteMarkerNetworkServerMessage message)
        {
            if (message.ListOfMarkersId.Count != 0)
            {
                Mission.Current.GetMissionBehavior<SaeBehavior>().RemoveMarkersFromList(message.ListOfMarkersId);
            }
        }
    }
}
