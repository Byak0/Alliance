using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Models
{
	[Serializable]
	public class Act
	{
		[ScenarioEditor(label: "Act name")]
		public LocalizedString Name = new("Act 1");

		[ScenarioEditor(label: "Act description", tooltip: "Short description. Will be displayed to players at the beginning of the act and when pressing tab.")]
		public LocalizedString Description = new("There is stuff to do, follow objectives !");

		[ScenarioEditor(label: "Load Map", tooltip: "If enabled, force a map load before starting the act. Disable it if you want to keep the map from the previous act.")]
		public bool LoadMap;

		[ScenarioEditor(label: "Map ID", tooltip: "ID of the map to load", dataType: ScenarioData.DataTypes.Map)]
		public string MapID;

		[ScenarioEditor(label: "Settings", tooltip: "Specify which game mode and what settings to apply for this act.")]
		public GameModeSettings ActSettings = new GameModeSettings();

		[ScenarioEditor(label: "Victory events", tooltip: "Define what happens on victory among the list of available events.")]
		public VictoryLogic VictoryLogic = new VictoryLogic();

		[ScenarioEditor(label: "Spawn behavior", tooltip: "Define how, when and where the players and IA must spawn.")]
		public SpawnLogic SpawnLogic = new SpawnLogic();

		[ScenarioEditor(label: "Objectives", tooltip: "List of objectives to complete in this act.")]
		public List<ObjectiveBase> Objectives;

		[ScenarioEditor(label: "Trigger actions", tooltip: "List of actions to trigger based on conditions.")]
		public List<ConditionalActionStruct> ConditionalActions = new List<ConditionalActionStruct>();

		public Act(LocalizedString name, LocalizedString desc, bool loadMap, string mapId, GameModeSettings actSettings, SpawnLogic spawnLogic, VictoryLogic victoryLogic)
		{
			Name = name;
			Description = desc;
			LoadMap = loadMap;
			MapID = mapId;
			ActSettings = actSettings;
			SpawnLogic = spawnLogic;
			VictoryLogic = victoryLogic;
			Objectives = new List<ObjectiveBase>();
		}

		public Act() { }

		public void RegisterObjectives()
		{
			foreach (ObjectiveBase objective in Objectives)
			{
				objective.Reset();
				objective.RegisterForUpdate();
			}
			foreach (ConditionalActionStruct conditionalAction in ConditionalActions)
			{
				foreach (Condition condition in conditionalAction.Conditions)
				{
					condition.Register();
				}
			}
		}

		public void UnregisterObjectives()
		{
			foreach (ObjectiveBase objective in Objectives)
			{
				objective.UnregisterForUpdate();
			}
			foreach (ConditionalActionStruct conditionalAction in ConditionalActions)
			{
				foreach (Condition condition in conditionalAction.Conditions)
				{
					condition.Unregister();
				}
			}
		}
	}
}
