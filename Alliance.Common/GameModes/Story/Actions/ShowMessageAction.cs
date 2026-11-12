using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
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
	[PhrasePreview("Show {MessageType}")]
	public class ShowMessageAction : ActionBase
	{
		public enum MessageFormat
		{
			SystemNotification,
			QuickInformation,
			Message
		}

		public LocalizedString Message = new("Message");
		public MessageFormat MessageType = MessageFormat.SystemNotification;

		public ShowMessageAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			ExecuteOnClient(context);
			return ActionTask.CompletedTask;
		}

		public override void ExecuteClient()
		{
			switch (MessageType)
			{
				case MessageFormat.SystemNotification:
					InformationManager.AddSystemNotification(Message.LocalizedText);
					break;
				case MessageFormat.QuickInformation:
					MBInformationManager.AddQuickInformation(new TextObject(Message.LocalizedText, null), 0, null);
					break;
				case MessageFormat.Message:
					InformationManager.DisplayMessage(new InformationMessage(Message.LocalizedText, Color.White));
					break;
			}
		}
	}
}