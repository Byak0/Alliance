using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Objectives;
using System;
using System.Collections.Generic;
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

		[ConfigProperty(label: "Objectives", tooltip: "List of objectives to complete in this act.")]
		public List<ObjectiveBase> Objectives = new List<ObjectiveBase>();

		[ConfigProperty(label: "Scripted events", tooltip: "You can define various events that can be triggered based on conditions.")]
		public List<ScriptedEvent> ConditionalActions = new List<ScriptedEvent>();

		[ConfigProperty(label: "Victory events", tooltip: "Events triggered upon victory.")]
		public VictoryLogic VictoryLogic = new VictoryLogic();

		public Act(LocalizedString name, LocalizedString desc, bool loadMap, string mapId, ScenarioGameModeSettings actSettings, SpawnLogic spawnLogic, VictoryLogic victoryLogic)
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
			foreach (ScriptedEvent conditionalAction in ConditionalActions)
			{
				conditionalAction.Register(WeakGameEntity.Invalid);
			}
		}

		public void UnregisterObjectives()
		{
			foreach (ObjectiveBase objective in Objectives)
			{
				objective.UnregisterForUpdate();
			}
			foreach (ScriptedEvent conditionalAction in ConditionalActions)
			{
				foreach (Condition condition in conditionalAction.Conditions)
				{
					condition.Unregister();
				}
			}
		}
	}
}
