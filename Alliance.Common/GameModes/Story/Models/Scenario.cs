using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Objectives;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Models
{
	public class Scenario
	{
		[ConfigProperty(false)]
		public string Id;
		[ConfigProperty(false)]
		public int Version;
		[ConfigProperty(false)]
		public DateTime LastEditedAt;
		[ConfigProperty(false)]
		public string LastEditedBy;

		[ConfigProperty(label: "Scenario name")]
		public LocalizedString Name = new("My cool scenario");

		[ConfigProperty(label: "Scenario description", tooltip: "Short description, displayed to players at the beginning of the scenario.")]
		public LocalizedString Description = new("Make great things in this super scenario");

		[ConfigProperty(label: "Acts", tooltip: "Acts are the 'chapters' of the scenario. You can add as many as you want. A scenario must have at least one act to work.")]
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

		public static Scenario CreateDefaultScenario()
		{
			Scenario scenario = new Scenario(
				name: new LocalizedString("Name"),
				desc: new LocalizedString("Description")
				);

			List<ActionBase> act1DisplayResultsActions = new List<ActionBase>()
			{
				new ShowResultScreenAction()
			};
			List<ActionBase> act1CompletedActions = new List<ActionBase>()
			{
				new StartGameAction("", new GameModeSettings())
			};

			VictoryLogic act1VictoryLogic = new VictoryLogic(act1DisplayResultsActions, act1CompletedActions);
			Act act1 = new Act(
				name: new LocalizedString("Act 1"),
				desc: new LocalizedString(""),
				loadMap: false,
				mapId: "",
				actSettings: new ScenarioGameModeSettings(),
				spawnLogic: new SpawnLogic(),
				victoryLogic: act1VictoryLogic
				);
			KillAllObjective act1objective1 = new KillAllObjective(
				BattleSideEnum.Defender,
				new LocalizedString("Kill all attackers"),
				new LocalizedString(""),
				true, false);
			KillAllObjective act1objective2 = new KillAllObjective(
				BattleSideEnum.Attacker,
				new LocalizedString("Kill all defenders"),
				new LocalizedString(""),
				true, false);
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			scenario.Acts.Add(act1);

			return scenario;
		}
	}
}
