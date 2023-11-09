using Alliance.Client.Extensions.SAE.Behaviors;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.SAE.Handlers
{
    public class SaeCreateMarkerHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SaeCreateMarkersNetworkServerMessage>(OnCreateMarkerMessageReceived);
        }

        public void OnCreateMarkerMessageReceived(SaeCreateMarkersNetworkServerMessage message)
        {
            if (message.MarkerList.Count != 0)
            {
                Mission.Current.GetMissionBehavior<SaeBehavior>().AddMarkerFromList(message.MarkerList);
            }
        }
    }
}
