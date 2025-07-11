using Alliance.Client.Extensions.AdminMenu.ViewModels;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using Color = TaleWorlds.Library.Color;

namespace Alliance.Client.Extensions.AdminMenu.Handlers
{
	public class AdminMenuHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<AdminServerLog>(HandleLogMessage);
			reg.Register<SendNotification>(HandleNotification);
		}

		public void HandleNotification(SendNotification notification)
		{
			switch (notification.NotificationType)
			{
				case 0:
					InformationManager.AddSystemNotification(notification.Text);
					break;
				case 1:
					MBInformationManager.AddQuickInformation(new TextObject(notification.Text, null), 0, null, "");
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
	}
}
