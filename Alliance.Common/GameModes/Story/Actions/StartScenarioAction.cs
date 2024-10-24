﻿using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a scenario.
	/// </summary>
	[Serializable]
	public class StartScenarioAction : ActionBase
	{
		[ScenarioEditor(label: "Scenario ID", tooltip: "ID of the scenario to load. You can leave it empty to use current scenario.")]
		public string ScenarioId;
		[ScenarioEditor(label: "Act index", tooltip: "Index of act to load.")]
		public int ActIndex;

		public StartScenarioAction(string scenarioId, int actIndex)
		{
			ScenarioId = scenarioId;
			ActIndex = actIndex;
		}

		public StartScenarioAction() { }
	}
}