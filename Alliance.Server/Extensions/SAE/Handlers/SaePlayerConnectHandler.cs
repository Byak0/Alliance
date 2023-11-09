using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE.Handlers
{
    /// <summary>
    /// This handler will handle message send by a client to the server
    /// So this code will only be executed by server
    /// </summary>
    public class SaePlayerConnectHandler
    {
        public SaePlayerConnectHandler() { }

        public bool OnSaeMessageReceived(NetworkCommunicator peer, SaeCreateMarkerNetworkClientMessage message)
        {

            return true;
        }
    }
}
