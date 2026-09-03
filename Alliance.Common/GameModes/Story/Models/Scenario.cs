using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.GameModes.Story.Validation;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

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

		[ConfigProperty(label: "Variables", tooltip: "Global variables available throughout the scenario. Variables can be used in conditions and actions via ValueSource fields.")]
		public List<ScenarioVariable> Variables = new List<ScenarioVariable>();

		[ConfigProperty(label: "Cinematics", tooltip: "Cinematics defined on this scenario, identified by their unique name. Reference them by name from a PlayCinematicAction or the act intro cinematic slot.")]
		public List<Cinematic> Cinematics = new List<Cinematic>();

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

			List<ActionBase> act1VictoryActions = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new EndScenarioAction()
			}; 

			VictoryLogic act1VictoryLogic = new VictoryLogic(act1VictoryActions);
			Act act1 = new Act(
				name: new LocalizedString("Act 1"),
				desc: new LocalizedString(""),
				loadMap: false,
				mapId: "",
				actSettings: new ScenarioGameModeSettings(),
				spawnLogic: new SpawnLogic(),
				victoryLogic: act1VictoryLogic
				);
			ValueSource<int> attackerCount = new FunctionCall<int>(
				new CountOfFunction {
					List = new FunctionCall<List<TaleWorlds.MountAndBlade.Agent>>(
						new AgentsInZoneFunction { Anywhere = true, Side = SideType.Attacker })
				});
			ValueSource<int> defenderCount = new FunctionCall<int>(
				new CountOfFunction {
					List = new FunctionCall<List<TaleWorlds.MountAndBlade.Agent>>(
						new AgentsInZoneFunction { Anywhere = true, Side = SideType.Defender })
				});

			Objective act1objective1 = new Objective(
				BattleSideEnum.Defender,
				new LocalizedString("Kill all attackers"),
				new LocalizedString(""),
				true, false,
				new SideEliminatedCondition(SideType.Attacker),
				new List<ProgressElement> {
					new TextElement(new LocalizedString("Enemies remaining: {0}"), new IntValue(attackerCount))
				});
			Objective act1objective2 = new Objective(
				BattleSideEnum.Attacker,
				new LocalizedString("Kill all defenders"),
				new LocalizedString(""),
				true, false,
				new SideEliminatedCondition(SideType.Defender),
				new List<ProgressElement> {
					new TextElement(new LocalizedString("Enemies remaining: {0}"), new IntValue(defenderCount))
				});
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			scenario.Acts.Add(act1);

			return scenario;
		}

		public List<ValidationIssue> Validate()
		{
			var issues = new List<ValidationIssue>();

			if (Acts == null || Acts.Count == 0)
			{
				issues.Add(new ValidationIssue(ValidationSeverity.Error, "Scenario", "No acts defined."));
				return issues;
			}

			// Variables are identified by name: duplicates would shadow each other in the variable stores.
			var variableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (ScenarioVariable variable in Variables ?? new List<ScenarioVariable>())
			{
				if (variable == null) continue;
				if (string.IsNullOrWhiteSpace(variable.Name))
					issues.Add(new ValidationIssue(ValidationSeverity.Error, "Scenario > Variables", "A variable has no name."));
				else if (!variableNames.Add(variable.Name))
					issues.Add(new ValidationIssue(ValidationSeverity.Error, "Scenario > Variables", $"Duplicate variable name '{variable.Name}'. Variable names must be unique."));
			}

			// Cinematics are identified by name: duplicates would break by-name references.
			var cinematicNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (Cinematic cinematic in Cinematics ?? new List<Cinematic>())
			{
				if (cinematic == null) continue;
				if (string.IsNullOrWhiteSpace(cinematic.Name))
					issues.Add(new ValidationIssue(ValidationSeverity.Error, "Scenario > Cinematics", "A cinematic has no name."));
				else if (!cinematicNames.Add(cinematic.Name))
					issues.Add(new ValidationIssue(ValidationSeverity.Error, "Scenario > Cinematics", $"Duplicate cinematic name '{cinematic.Name}'. Cinematic names must be unique."));
			}

			for (int i = 0; i < Acts.Count; i++)
			{
				if (Acts[i] == null) continue;
				issues.AddRange(Acts[i].Validate(i));
			}

			return issues;
		}
	}
}
