using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Actions;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Logic for handling victory conditions and results.
	/// </summary>
	[Serializable]
	public class VictoryLogic
	{
		[ConfigProperty(label: "Actions on victory", tooltip: "These actions will be triggered as soon as one side completed its objectives.")]
		public List<ActionBase> ActionsOnDisplayResults;

		[ConfigProperty(label: "Actions delayed", tooltip: "Actions triggered after a short delay")]
		public List<ActionBase> ActionsOnActCompleted;

		public VictoryLogic(List<ActionBase> displayResultsActions, List<ActionBase> actionsOnActCompleted)
		{
			ActionsOnDisplayResults = displayResultsActions;
			ActionsOnActCompleted = actionsOnActCompleted;
		}

		public VictoryLogic() { }

		public void OnDisplayResults(BattleSideEnum winner)
		{
			Log($"Winner is : {winner}", LogLevel.Debug);
			foreach (ActionBase action in ActionsOnDisplayResults)
			{
				action.Execute();
			}
		}

		public void OnActCompleted(BattleSideEnum winner)
		{
			foreach (ActionBase action in ActionsOnActCompleted)
			{
				action.Execute();
			}
		}
	}
}