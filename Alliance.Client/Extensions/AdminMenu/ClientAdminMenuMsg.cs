using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
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

		public static void RequestUpdateOptionsToServer(TWConfig nativeOptions, Config modOptions)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestUpdateOptions(nativeOptions, modOptions));
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
