using Alliance.Common.Core.Configuration.Models;
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
		[ConfigProperty(label: "Default unit (attacker)", tooltip: "Default character to spawn for attacker")]
		public string DefaultCharacterAttacker;
		[ConfigProperty(label: "Default unit (defender)", tooltip: "Default character to spawn for defender")]
		public string DefaultCharacterDefender;
		[ConfigProperty(label: "Default spawn (attacker)", tooltip: "By default, use spawn positions with this tag for attacker")]
		public string DefaultSpawnTagAttacker;
		[ConfigProperty(label: "Default spawn (defender)", tooltip: "By default, use spawn positions with this tag for defender")]
		public string DefaultSpawnTagDefender;
		[ConfigProperty(label: "Spawn type (attacker)", tooltip: "How to choose the spawn location for the attacker")]
		public LocationStrategy LocationStrategyAttacker;
		[ConfigProperty(label: "Spawn type (defender)", tooltip: "How to choose the spawn location for the defender")]
		public LocationStrategy LocationStrategyDefender;
		[ConfigProperty(label: "Number of lives (attacker)", tooltip: "Number of lives for attacker")]
		public int MaxLivesAttacker;
		[ConfigProperty(label: "Number of lives (defender)", tooltip: "Number of lives for defender")]
		public int MaxLivesDefender;
		[ConfigProperty(label: "Respawn type (attacker)", tooltip: "Define how attackers can respawn")]
		public RespawnStrategy RespawnStrategyAttacker;
		[ConfigProperty(label: "Respawn type (defender)", tooltip: "Define how defenders can respawn")]
		public RespawnStrategy RespawnStrategyDefender;
		[ConfigProperty(label: "Keep lives", tooltip: "If enabled, add number of lives on top of the lives left from previous act. Otherwise previous lives left are lost.")]
		public bool KeepLivesFromPreviousAct;
		[ConfigProperty(label: "Store agents", tooltip: "If enabled, store the state and positions of all agents at the end of the act.")]
		public bool StoreAgentsInfo;
		[ConfigProperty(label: "Use stored agents", tooltip: "If enabled, spawn the agents that were stored in a previous act.")]
		public bool UsePreviousActAgents;

		[XmlIgnore]
		public string[] DefaultCharacters
		{
			get => new[] { DefaultCharacterDefender, DefaultCharacterAttacker };
			set
			{
				if (value.Length >= 2)
				{
					DefaultCharacterDefender = value[0];
					DefaultCharacterAttacker = value[1];
				}
			}
		}

		[XmlIgnore]
		public string[] DefaultSpawnTags
		{
			get => new[] { DefaultSpawnTagDefender, DefaultSpawnTagAttacker };
			set
			{
				if (value.Length >= 2)
				{
					DefaultSpawnTagDefender = value[0];
					DefaultSpawnTagAttacker = value[1];
				}
			}
		}

		[XmlIgnore]
		public int[] MaxLives
		{
			get => new[] { MaxLivesDefender, MaxLivesAttacker };
			set
			{
				if (value.Length >= 2)
				{
					MaxLivesDefender = value[0];
					MaxLivesAttacker = value[1];
				}
			}
		}

		[XmlIgnore]
		public LocationStrategy[] LocationStrategies
		{
			get => new[] { LocationStrategyDefender, LocationStrategyAttacker };
			set
			{
				if (value.Length >= 2)
				{
					LocationStrategyDefender = value[0];
					LocationStrategyAttacker = value[1];
				}
			}
		}

		[XmlIgnore]
		public RespawnStrategy[] RespawnStrategies
		{
			get => new[] { RespawnStrategyDefender, RespawnStrategyAttacker };
			set
			{
				if (value.Length >= 2)
				{
					RespawnStrategyDefender = value[0];
					RespawnStrategyAttacker = value[1];
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
