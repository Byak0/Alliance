
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.AdminMenu
{
	public static class CommonAdminMsg
	{
		public static void SendNotificationToAll(string message, int notificationType)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SendNotification(message, notificationType));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public static void SendNotificationToAll(string message)
		{
			SendNotificationToAll(message, 0);
		}

		public static void SendInformationToAll(string message)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SendNotification(message, 1));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public static void SendNotificationToPeerAsServer(NetworkCommunicator target, string message)
		{
			GameNetwork.BeginModuleEventAsServer(target);
			GameNetwork.WriteMessage(new SendNotification(message, 0));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
