using Alliance.Common.Core.Security;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AdminMenu
{
	internal static class ServerAdminMenuMsg
	{
		public static void SendMessageToAllAdmins(string message, AdminServerLog.ColorList color)
		{
			foreach (NetworkCommunicator peer in PlayerStore.Instance.OnlineAdmins)
			{
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(new AdminServerLog(message, color));
				GameNetwork.EndModuleEventAsServer();
			}
		}

		public static void SendMessageToAdmin(NetworkCommunicator player, string message, AdminServerLog.ColorList color)
		{
			GameNetwork.BeginModuleEventAsServer(player);
			GameNetwork.WriteMessage(new AdminServerLog(message, color));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
