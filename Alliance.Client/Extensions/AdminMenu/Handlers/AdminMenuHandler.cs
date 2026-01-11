using System.Collections.Generic;
using Alliance.Client.Extensions.AdminMenu.ViewModels;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using JetBrains.Annotations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Color = TaleWorlds.Library.Color;

namespace Alliance.Client.Extensions.AdminMenu.Handlers
{
	public class AdminMenuHandler : IHandlerRegister
	{
		public static Dictionary<NetworkCommunicator, (int, int, int)> AgentTkCount = new Dictionary<NetworkCommunicator, (int, int, int)>();
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<AdminServerLog>(HandleLogMessage);
			reg.Register<SendNotification>(HandleNotification);

			// handler to get dict with Tk info
			reg.Register<SyncTk>(HandleSyncTk);
		}

		public void HandleNotification(SendNotification notification)
		{
			switch (notification.NotificationType)
			{
				case 0:
					InformationManager.AddSystemNotification(notification.Text);
					break;
				case 1:
					MBInformationManager.AddQuickInformation(new TextObject(notification.Text, null), 0, null);
					break;
				case 2:
					InformationManager.DisplayMessage(new InformationMessage(notification.Text, Color.White));
					break;
			}
		}

		public void HandleLogMessage(AdminServerLog logger)
		{
			AdminInstance.UpdateServerMessage(new ServerMessageVM(logger.LogMessage, logger.Color));
		}
		public static void HandleSyncTk(SyncTk message)
		{
			// Read Notification from server :
			foreach (var kvp in message.AgentTkData)
			{
				NetworkCommunicator peer = kvp.Key;
				var (tkCount, tkDamage, tkKill) = kvp.Value;

				InformationManager.DisplayMessage(new InformationMessage(
					$"Receive agents with TK : network communicator, tkCount, tkDamage, tkKill"
				));

				// Recreate dictionnary on client side :
				AgentTkCount[peer] = (tkCount, tkDamage, tkKill);
			}
		}
	}
}
