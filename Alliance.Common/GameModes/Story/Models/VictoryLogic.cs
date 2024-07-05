using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Models
{
	public class VictoryLogic
	{
		public delegate void OnActVictoryDelegate(BattleSideEnum side);
		public event OnActVictoryDelegate OnActShowResults;
		public event OnActVictoryDelegate OnActCompleted;

		public VictoryLogic(OnActVictoryDelegate onActShowResults, OnActVictoryDelegate onActCompleted)
		{
			OnActShowResults = onActShowResults;
			OnActCompleted = onActCompleted;
		}

		public void HandleResults(BattleSideEnum winner)
		{
			OnActShowResults?.Invoke(winner);
		}

		public void HandleActCompleted(BattleSideEnum winner)
		{
			OnActCompleted?.Invoke(winner);
		}
	}
}