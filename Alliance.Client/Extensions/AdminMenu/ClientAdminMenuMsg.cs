using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.AdminMenu
{
	public static class ClientAdminMenuMsg
	{
		public static void SendMessageToServer(AdminClient adminRequest)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(adminRequest);
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
