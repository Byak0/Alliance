using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE.Handlers
{
	public class SaeServerHandler : IHandlerRegister
	{
		private SaeCreateMarkerHandler _saeCreateHandler;
		private SaeDeleteMarkerHandler _saeDeleteHandler;
		private SaeGetMarkersHandler _saeGetMarkersHandler;
		private SaeDebugHandler _saeDebugHandler;
		private SaeHandler _saeHandler;

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			_saeCreateHandler ??= new SaeCreateMarkerHandler();
			_saeDeleteHandler ??= new SaeDeleteMarkerHandler();
			_saeGetMarkersHandler ??= new SaeGetMarkersHandler();
			_saeDebugHandler ??= new SaeDebugHandler();
			_saeHandler ??= new SaeHandler();

			reg.Register<SaeCreateMarkerNetworkClientMessage>(_saeCreateHandler.OnSaeCreateMarkerMessageReceived);
			reg.Register<SaeCrouchNetworkClientMessage>(_saeCreateHandler.OnCrouchMessageReceived);
			reg.Register<SaeDeleteMarkerForAllNetworkClientMessage>(_saeDeleteHandler.OnSaeDeleteForAllClientMessageReceived);
			reg.Register<SaeDeleteAllMarkersForAllNetworkClientMessage>(_saeDeleteHandler.OnSaeDeleteAllForAllClientMessageReceived);
			reg.Register<SaeGetMarkersNetworkClientMessage>(_saeGetMarkersHandler.OnSaeMessageReceived);
			reg.Register<SaeDebugNetworkClientMessage>(_saeDebugHandler.OnSaeMessageReceived);
			reg.Register<SaeCreateDynamicMarkerNetworkClientMessage>(_saeHandler.OnSaeCreateMarkerMessageReceived);
		}
	}
}
