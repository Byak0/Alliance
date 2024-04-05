using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.Extensions.RTSCamera.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Server.Extensions.RTSCamera.Handlers
{
    public class CameraPositionsHandler : IHandlerRegister
    {
        public void Register(NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestUpdateCameraPosition>(HandleUpdateCameraPosition);
        }

        private bool HandleUpdateCameraPosition(NetworkCommunicator peer, RequestUpdateCameraPosition message)
        {
            return CameraPositionsModel.Instance.HandleUpdateCameraPosition(peer, message);
        }
    }
}
