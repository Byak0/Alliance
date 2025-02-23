using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Display some text.
	/// </summary>
	[Serializable]
	public class ShowMessageAction : ActionBase
	{
		public LocalizedString Message = new("Message");
		public MessageFormat MessageType = MessageFormat.SystemNotification;

		public ShowMessageAction() { }

		public override void Execute()
		{
			switch (MessageType)
			{
				case MessageFormat.SystemNotification:
					InformationManager.AddSystemNotification(Message.LocalizedText);
					break;
				case MessageFormat.QuickInformation:
					MBInformationManager.AddQuickInformation(new TextObject(Message.LocalizedText, null), 0, null, "");
					break;
				case MessageFormat.Message:
					InformationManager.DisplayMessage(new InformationMessage(Message.LocalizedText, Color.White));
					break;
			}
		}
	}

	public enum MessageFormat
	{
		SystemNotification,
		QuickInformation,
		Message
	}
}