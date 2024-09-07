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

		public override bool Evaluate(ScenarioManager context)
		{
			return context.CurrentWinner == ExpectedWinner;
		}
	}
}
