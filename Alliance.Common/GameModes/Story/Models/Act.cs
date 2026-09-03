using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.GameModes.Story.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Models
{
	[Serializable]
	[PhrasePreview("{Name}{?LoadMap: — {MapID}}")]
	public class Act
	{
		[ConfigProperty(label: "Act name")]
		public LocalizedString Name = new("Act 1");

		[ConfigProperty(label: "Act description", tooltip: "Short description. Will be displayed to players at the beginning of the act and when pressing tab.")]
		public LocalizedString Description = new("There is stuff to do, follow objectives !");

		[ConfigProperty(label: "Load Map", tooltip: "If enabled, force a map load before starting the act. Disable it if you want to keep the map currently playing.")]
		public bool LoadMap = false;

		[ConfigProperty(label: "Map ID", tooltip: "ID of the map to load", dataType: AllianceData.DataTypes.Map, dependency: nameof(LoadMap))]
		public string MapID;

		[ConfigProperty(label: "Generic settings", tooltip: "Define native and mod settings for this act.")]
		public ScenarioGameModeSettings ActSettings = new ScenarioGameModeSettings();

		[ConfigProperty(label: "Spawn settings", tooltip: "Define how, when and where the players and IA must spawn.")]
		public SpawnLogic SpawnLogic = new SpawnLogic();

		[ConfigProperty(label: "Zones", tooltip: "Define specific zones for this act. Can be used anywhere in the act.", category: "Zones")]
		public List<NamedZone> Zones = new List<NamedZone>();

		[ConfigProperty(label: "Objectives", tooltip: "List of objectives to complete in this act.", category: "Victory conditions")]
		public List<Objective> Objectives = new List<Objective>();

		[ConfigProperty(label: "Victory events", tooltip: "Events triggered upon victory.", category: "Victory conditions")]
		[InlineContent]
		public VictoryLogic VictoryLogic = new VictoryLogic();

		[ConfigProperty(label: "Scripted events", tooltip: "You can define various events that can be triggered based on conditions.", category: "Additional events")]
		public List<ScriptedEvent> ConditionalActions = new List<ScriptedEvent>();

		public Act(LocalizedString name, LocalizedString desc, bool loadMap, string mapId, ScenarioGameModeSettings actSettings, SpawnLogic spawnLogic, VictoryLogic victoryLogic)
		{
			Name = name;
			Description = desc;
			LoadMap = loadMap;
			MapID = mapId;
			ActSettings = actSettings;
			SpawnLogic = spawnLogic;
			VictoryLogic = victoryLogic;
			Objectives = new List<Objective>();
		}

		public Act() { }

		public void RegisterObjectives()
		{
			foreach (Objective objective in Objectives)
			{
				objective.Reset();
			}
			foreach (ScriptedEvent conditionalAction in ConditionalActions)
			{
				conditionalAction.Register(WeakGameEntity.Invalid);
			}
			foreach (NamedZone namedZone in Zones)
			{
				namedZone.Zone?.Register(WeakGameEntity.Invalid);
			}
			if (VictoryLogic?.ActionsOnVictory != null)
			{
				foreach (var action in VictoryLogic.ActionsOnVictory)
				{
					action.Register(WeakGameEntity.Invalid);
				}
			}
		}

		public void UnregisterObjectives()
		{
			foreach (Objective objective in Objectives)
			{
				objective.Unregister();
			}
			foreach (ScriptedEvent conditionalAction in ConditionalActions)
			{
				foreach (Condition condition in conditionalAction.Conditions)
				{
					condition.Unregister();
				}
				foreach (var action in conditionalAction.Actions)
				{
					action.UnassignActionId();
				}
			}
			if (VictoryLogic?.ActionsOnVictory != null)
			{
				foreach (var action in VictoryLogic.ActionsOnVictory)
				{
					action.UnassignActionId();
				}
			}
		}

		public List<ValidationIssue> Validate(int actIndex)
		{
			var issues = new List<ValidationIssue>();
			string actLabel = $"Act {actIndex + 1}" + (Name?.GetText("English") != null ? $" '{Name.GetText("English")}'" : "");

			if (Objectives == null || Objectives.Count == 0)
			{
				issues.Add(new ValidationIssue(ValidationSeverity.Error, actLabel, "No objectives. The act cannot be won."));
			}
			else
			{
				var attackerObjs = Objectives.Where(o => o.Side == BattleSideEnum.Attacker).ToList();
				var defenderObjs = Objectives.Where(o => o.Side == BattleSideEnum.Defender).ToList();

				if (attackerObjs.Count == 0)
					issues.Add(new ValidationIssue(ValidationSeverity.Info, actLabel, "No objectives for Attackers — only Defenders can win this act."));
				if (defenderObjs.Count == 0)
					issues.Add(new ValidationIssue(ValidationSeverity.Info, actLabel, "No objectives for Defenders — only Attackers can win this act."));

				var instantWinAttacker = attackerObjs.Count(o => o.InstantActWin);
				var instantWinDefender = defenderObjs.Count(o => o.InstantActWin);
				if (instantWinAttacker > 1)
					issues.Add(new ValidationIssue(ValidationSeverity.Info, actLabel, $"{instantWinAttacker} Attacker objectives have Instant Win — first to complete wins (race condition)."));
				if (instantWinDefender > 1)
					issues.Add(new ValidationIssue(ValidationSeverity.Info, actLabel, $"{instantWinDefender} Defender objectives have Instant Win — first to complete wins (race condition)."));

				foreach (var obj in Objectives)
				{
					string objLabel = $"{actLabel} > Objective '{obj.Name?.GetText("English") ?? "?"}'";
					issues.AddRange(obj.Validate(objLabel));
				}
			}

			if (LoadMap && string.IsNullOrWhiteSpace(MapID))
				issues.Add(new ValidationIssue(ValidationSeverity.Error, actLabel, "Load Map is enabled but no Map ID is set."));

			// Start flow validation
			if (SpawnLogic != null)
			{
				bool menuDefined = SpawnLogic.PlayerSpawnMenu?.Teams != null && SpawnLogic.PlayerSpawnMenu.Teams.Count > 0;
				if (SpawnLogic.CharacterAssignment == CharacterAssignmentMode.Auto
					&& SpawnLogic.CharacterAutoRule == CharacterAutoAssignmentRule.DefaultUnits
					&& (string.IsNullOrWhiteSpace(SpawnLogic.DefaultCharacterAttacker?.Resolve()) || string.IsNullOrWhiteSpace(SpawnLogic.DefaultCharacterDefender?.Resolve())))
				{
					issues.Add(new ValidationIssue(ValidationSeverity.Error, actLabel, "Character assignment = Auto / Default units but no default unit is set for both sides. Players would spawn without a character."));
				}
				if (SpawnLogic.CharacterAssignment == CharacterAssignmentMode.Auto
					&& SpawnLogic.CharacterAutoRule == CharacterAutoAssignmentRule.RandomFromMenu
					&& !menuDefined)
				{
					issues.Add(new ValidationIssue(ValidationSeverity.Error, actLabel, "Character assignment = Auto / Random from menu but the player spawn menu is empty. Players would fall back to the default units."));
				}
				if (SpawnLogic.CharacterAssignment == CharacterAssignmentMode.PlayerSelection && !menuDefined)
				{
					issues.Add(new ValidationIssue(ValidationSeverity.Warning, actLabel, "Character assignment = Player selection but the player spawn menu is empty - the default units will be used instead."));
				}
				if (SpawnLogic.TeamAssignment == TeamAssignmentMode.Auto && SpawnLogic.TeamAutoRule == TeamAutoAssignmentRule.Balanced)
				{
					issues.Add(new ValidationIssue(ValidationSeverity.Info, actLabel, "Team auto rule = Balanced: sides are evened out at spawn start, overriding the players' team selection."));
				}
			}

			return issues;
		}
	}
}
