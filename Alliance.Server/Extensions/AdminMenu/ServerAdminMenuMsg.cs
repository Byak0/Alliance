using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AdminMenu
{
	internal static class ServerAdminMenuMsg
	{
		public static void SendMessageToClient(NetworkCommunicator targetPeer, string message, AdminServerLog.ColorList color, bool forAdmin = false)
		{
			if (!forAdmin)
			{
				GameNetwork.BeginModuleEventAsServer(targetPeer);
				GameNetwork.WriteMessage(new AdminServerLog(message, color));
				GameNetwork.EndModuleEventAsServer();
			}

			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(new AdminServerLog(message, color));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}
}
