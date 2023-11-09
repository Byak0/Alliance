using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE.Handlers
{
    public class SaeServerHandler : IHandlerRegister
    {
        public SaeCreateMarkerHandler saeCreateHandler = new SaeCreateMarkerHandler();
        SaeDeleteMarkerHandler saeDeleteHandler = new SaeDeleteMarkerHandler();
        SaeGetMarkersHandler saeGetMarkersHandler = new SaeGetMarkersHandler();
        SaeDebugHandler saeDebugHandler = new SaeDebugHandler();
        SaeHandler saeHandler = new SaeHandler();

        public SaeServerHandler()
        {

        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SaeCreateMarkerNetworkClientMessage>(saeCreateHandler.OnSaeCreateMarkerMessageReceived);
            reg.Register<SaeCrouchNetworkClientMessage>(saeCreateHandler.OnCrouchMessageReceived);
            reg.Register<SaeDeleteMarkerForAllNetworkClientMessage>(saeDeleteHandler.OnSaeDeleteForAllClientMessageReceived);
            reg.Register<SaeDeleteAllMarkersForAllNetworkClientMessage>(saeDeleteHandler.OnSaeDeleteAllForAllClientMessageReceived);
            reg.Register<SaeGetMarkersNetworkClientMessage>(saeGetMarkersHandler.OnSaeMessageReceived);
            reg.Register<SaeDebugNetworkClientMessage>(saeDebugHandler.OnSaeMessageReceived);
            reg.Register<SaeCreateDynamicMarkerNetworkClientMessage>(saeHandler.OnSaeCreateMarkerMessageReceived);
        }
    }
}
