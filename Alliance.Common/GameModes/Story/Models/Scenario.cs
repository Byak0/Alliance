using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Models
{
	public class Scenario
	{
		[ScenarioEditor(false)]
		public string Id;
		[ScenarioEditor(false)]
		public int Version;
		[ScenarioEditor(false)]
		public DateTime LastEditedAt;
		[ScenarioEditor(false)]
		public string LastEditedBy = "somebody";

		[ScenarioEditor(label: "Scenario name")]
		public LocalizedString Name = new("My cool scenario");

		[ScenarioEditor(label: "Scenario description", tooltip: "Short description, displayed to players at the beginning of the scenario.")]
		public LocalizedString Description = new("Make great things in this super scenario");

		[ScenarioEditor(label: "Acts", tooltip: "Acts are the 'chapters' of the scenario. You can add as many as you want. A scenario must have at least one act to work.")]
		public List<Act> Acts;

		public Scenario(LocalizedString name, LocalizedString desc)
		{
			// TODO : better id generation
			Id = Guid.NewGuid().ToString();
			Name = name;
			Description = desc;
			Acts = new List<Act>();
		}

		public Scenario() { }
	}
}
