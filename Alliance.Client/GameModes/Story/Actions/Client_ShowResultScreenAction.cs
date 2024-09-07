using Alliance.Client.GameModes.Story.Views;
using Alliance.Common.GameModes.Story.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.GameModes.Story.Actions
{
	/// <summary>
	/// Action for showing a result screen with text.
	/// </summary>
	public class Client_ShowResultScreenAction : ShowResultScreenAction
	{
		public Client_ShowResultScreenAction() : base() { }

		public override void Execute()
		{
			if (Mission.Current.PlayerTeam == null)
			{
				return;
			}
			BattleSideEnum playerSide = Mission.Current.PlayerTeam.Side;
			ScenarioView sv = Mission.Current.GetMissionBehavior<ScenarioView>();
			bool isWinner = ScenarioPlayer.Instance.CurrentWinner == playerSide;
			string title;
			string description;
			if (playerSide == BattleSideEnum.Attacker)
			{
				title = isWinner ? TitleAttackerWin.LocalizedText : TitleAttackerLost.LocalizedText;
				description = isWinner ? TextAttackerWin.LocalizedText : TextAttackerLost.LocalizedText;
			}
			else
			{
				title = isWinner ? TitleDefenderWin.LocalizedText : TitleDefenderLost.LocalizedText;
				description = isWinner ? TextDefenderWin.LocalizedText : TextDefenderLost.LocalizedText;
			}
			Color color = isWinner ? new Color(0.4f, 0.7f, 0.1f) : new Color(0.7f, 0.1f, 0.1f);
			sv.ShowResultScreen(title, description, color);
		}
	}
}