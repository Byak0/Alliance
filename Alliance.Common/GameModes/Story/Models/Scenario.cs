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
		public string LastEditedBy;

		[ScenarioEditor(label: "Scenario name")]
		public LocalizedString Name = new("My cool scenario");

		[ScenarioEditor(label: "Scenario description", tooltip: "Short description, displayed to players at the beginning of the scenario.")]
		public LocalizedString Description = new("Make great things in this super scenario");

		[ScenarioEditor(label: "Acts", tooltip: "Acts are the 'chapters' of the scenario. You can add as many as you want. A scenario must have at least one act to work.")]
		public List<Act> Acts;

		public Scenario(LocalizedString name, LocalizedString desc)
		{
			// Generate a random ID for the scenario
			// 8 characters long means 1 in 10 billion chance of collision for 100,000 generated IDs
			// We merge Name with ID on save to ensure uniqueness
			Id = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
				  .Substring(0, 8)
				  .Replace("/", "_").Replace("+", "-"); // Ensure it's filename safe
			Name = name;
			Description = desc;
			Acts = new List<Act>();
		}

		public Scenario() { }
	}
}
