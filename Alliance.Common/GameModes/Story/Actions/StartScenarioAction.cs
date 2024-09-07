using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a scenario.
	/// </summary>
	[Serializable]
	public class StartScenarioAction : ActionBase
	{
		public string ScenarioId;
		public int ActIndex;

		public StartScenarioAction(string scenarioId, int actIndex)
		{
			ScenarioId = scenarioId;
			ActIndex = actIndex;
		}

		public StartScenarioAction() { }

		public override void Execute() { }
	}
}