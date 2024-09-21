using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Show a result screen with text.
	/// </summary>
	[Serializable]
	public class ShowResultScreenAction : ActionBase
	{
		public LocalizedString TitleAttackerWin = new("Attacker won !");
		public LocalizedString TextAttackerWin = new("The attacker has won the act !");
		public LocalizedString TitleDefenderLost = new("Defender lost !");
		public LocalizedString TextDefenderLost = new("The defender has lost the act !");

		public LocalizedString TitleDefenderWin = new("Defender won !");
		public LocalizedString TextDefenderWin = new("The defender has won the act !");
		public LocalizedString TitleAttackerLost = new("Attacker lost !");
		public LocalizedString TextAttackerLost = new("The attacker has lost the act !");

		public ShowResultScreenAction() { }
	}
}