using Alliance.Common.GameModes.Story.Utilities;
using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Conditions
{
	public class VictoryCondition : Condition
	{
		public BattleSideEnum ExpectedWinner;

		public VictoryCondition(BattleSideEnum expectedWinner)
		{
			ExpectedWinner = expectedWinner;
		}

		public VictoryCondition() { }

		public override bool Evaluate(VariableStore context)
		{
			return ScenarioManager.Instance?.CurrentWinner == ExpectedWinner;
		}
	}
}
