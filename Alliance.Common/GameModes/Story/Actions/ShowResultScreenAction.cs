using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Show a result screen with text.
	/// </summary>
	[Serializable]
	[PhrasePreview("Result screen")]
	public class ShowResultScreenAction : ActionBase
	{
		[ConfigProperty(label: "Win - Title", tooltip: "Title displayed to attacker when they win.", category: "Attacker")]
		public LocalizedString TitleAttackerWin = new("Attacker won !");
		[ConfigProperty(label: "Win - Text", tooltip: "Text displayed to attacker when they win.", category: "Attacker")]
		public LocalizedString TextAttackerWin = new("The attacker has won the act !");
		[ConfigProperty(label: "Lose - Title", tooltip: "Title displayed to attacker when they lose.", category: "Attacker")]
		public LocalizedString TitleAttackerLost = new("Attacker lost !");
		[ConfigProperty(label: "Lose - Text", tooltip: "Text displayed to attacker when they lose.", category: "Attacker")]
		public LocalizedString TextAttackerLost = new("The attacker has lost the act !");

		[ConfigProperty(label: "Win - Title", tooltip: "Title displayed to defender when they win.", category: "Defender")]
		public LocalizedString TitleDefenderWin = new("Defender won !");
		[ConfigProperty(label: "Win - Text", tooltip: "Text displayed to defender when they win.", category: "Defender")]
		public LocalizedString TextDefenderWin = new("The defender has won the act !");
		[ConfigProperty(label: "Lose - Title", tooltip: "Title displayed to defender when they lose.", category: "Defender")]
		public LocalizedString TitleDefenderLost = new("Defender lost !");
		[ConfigProperty(label: "Lose - Text", tooltip: "Text displayed to defender when they lose.", category: "Defender")]
		public LocalizedString TextDefenderLost = new("The defender has lost the act !");

		public ShowResultScreenAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			ExecuteOnClient(context);
			return ActionTask.CompletedTask;
		}
	}
}