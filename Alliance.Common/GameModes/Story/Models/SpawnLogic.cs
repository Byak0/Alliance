using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Store informations about how, when and where the players and IA must spawn.
	/// </summary>
	[Serializable]
	public class SpawnLogic
	{
		[ScenarioEditor(label: "Attacker default character", tooltip: "Default character to spawn for attacker")]
		public string DefaultCharacterAttacker;
		[ScenarioEditor(label: "Defender default character", tooltip: "Default character to spawn for defender")]
		public string DefaultCharacterDefender;
		[ScenarioEditor(label: "Attacker default spawn location", tooltip: "By default, use spawn positions with this tag for attacker")]
		public string DefaultSpawnTagAttacker;
		[ScenarioEditor(label: "Defender default spawn location", tooltip: "By default, use spawn positions with this tag for defender")]
		public string DefaultSpawnTagDefender;
		[ScenarioEditor(label: "Attacker location type", tooltip: "How to choose the spawn location for the attacker")]
		public LocationStrategy LocationStrategyAttacker;
		[ScenarioEditor(label: "Defender location type", tooltip: "How to choose the spawn location for the defender")]
		public LocationStrategy LocationStrategyDefender;
		[ScenarioEditor(label: "Attacker lives", tooltip: "Number of lives for attacker")]
		public int MaxLivesAttacker;
		[ScenarioEditor(label: "Defender lives", tooltip: "Number of lives for defender")]
		public int MaxLivesDefender;
		[ScenarioEditor(label: "Attacker respawn", tooltip: "Define how attackers can respawn")]
		public RespawnStrategy RespawnStrategyAttacker;
		[ScenarioEditor(label: "Defender respawn", tooltip: "Define how defenders can respawn")]
		public RespawnStrategy RespawnStrategyDefender;
		[ScenarioEditor(label: "Keep lives", tooltip: "If enabled, add number of lives on top of the lives left from previous act. Otherwise previous lives left are lost.")]
		public bool KeepLivesFromPreviousAct;
		[ScenarioEditor(label: "Store agents", tooltip: "If enabled, store the state and positions of all agents at the end of the act.")]
		public bool StoreAgentsInfo;
		[ScenarioEditor(label: "Use stored agents", tooltip: "If enabled, spawn the agents that were stored in a previous act.")]
		public bool UsePreviousActAgents;


		[XmlIgnore]
		public string[] DefaultCharacters
		{
			get => new[] { DefaultCharacterAttacker, DefaultCharacterDefender };
			set
			{
				if (value.Length >= 2)
				{
					DefaultCharacterAttacker = value[0];
					DefaultCharacterDefender = value[1];
				}
			}
		}

		[XmlIgnore]
		public string[] DefaultSpawnTags
		{
			get => new[] { DefaultSpawnTagAttacker, DefaultSpawnTagDefender };
			set
			{
				if (value.Length >= 2)
				{
					DefaultSpawnTagAttacker = value[0];
					DefaultSpawnTagDefender = value[1];
				}
			}
		}

		[XmlIgnore]
		public int[] MaxLives
		{
			get => new[] { MaxLivesAttacker, MaxLivesDefender };
			set
			{
				if (value.Length >= 2)
				{
					MaxLivesAttacker = value[0];
					MaxLivesDefender = value[1];
				}
			}
		}

		[XmlIgnore]
		public LocationStrategy[] LocationStrategies
		{
			get => new[] { LocationStrategyAttacker, LocationStrategyDefender };
			set
			{
				if (value.Length >= 2)
				{
					LocationStrategyAttacker = value[0];
					LocationStrategyDefender = value[1];
				}
			}
		}

		[XmlIgnore]
		public RespawnStrategy[] RespawnStrategies
		{
			get => new[] { RespawnStrategyAttacker, RespawnStrategyDefender };
			set
			{
				if (value.Length >= 2)
				{
					RespawnStrategyAttacker = value[0];
					RespawnStrategyDefender = value[1];
				}
			}
		}

		public SpawnLogic(string[] defaultCharacters, string[] spawnTags, bool storeAgentsInfo, bool usePreviousActAgents, int[] maxLives, bool keepLivesFromPreviousAct, LocationStrategy[] locationStrategies, RespawnStrategy[] respawnStrategies)
		{
			DefaultCharacters = defaultCharacters;
			DefaultSpawnTags = spawnTags;
			StoreAgentsInfo = storeAgentsInfo;
			UsePreviousActAgents = usePreviousActAgents;
			MaxLives = maxLives;
			KeepLivesFromPreviousAct = keepLivesFromPreviousAct;
			LocationStrategies = locationStrategies;
			RespawnStrategies = respawnStrategies;
		}

		public SpawnLogic() { }
	}

	public enum LocationStrategy
	{
		OnlyTags,
		OnlyFlags,
		TagsThenFlags,
		PlayerChoice,
	}

	public enum RespawnStrategy
	{
		NoRespawn,
		MaxLivesPerTeam,
		MaxLivesPerPlayer,
	}
}
