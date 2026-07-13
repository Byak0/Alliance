using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using System;
using System.Xml.Serialization;
using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Store informations about how, when and where the players and IA must spawn.
	/// </summary>
	[Serializable]
	public class SpawnLogic
	{
		[ConfigProperty(label: "Player spawn menu", tooltip: "Define the player spawn menu for this act. Set teams, formations and available characters.")]
		public PlayerSpawnMenu PlayerSpawnMenu = new PlayerSpawnMenu();
		[ConfigProperty(label: "Default unit (attacker)", tooltip: "Default character to spawn for attacker. Only used if the player spawn menu is undefined.", dataType: AllianceData.DataTypes.Character)]
		public string DefaultCharacterAttacker = "mp_heavy_infantry_empire_hero";
		[ConfigProperty(label: "Default unit (defender)", tooltip: "Default character to spawn for defender. Only used if the player spawn menu is undefined.", dataType: AllianceData.DataTypes.Character)]
		public string DefaultCharacterDefender = "mp_heavy_infantry_vlandia_hero";
		[ConfigProperty(label: "Officer selection strategy", tooltip: "Define how the officer is selected. You must define officer characters in the player spawn menu.")]
		public OfficerSelectionStrategy OfficerSelectionStrategy = OfficerSelectionStrategy.PlayerVote;
		[ConfigProperty(label: "Time before initial spawn", tooltip: "Time in seconds before the original spawn. Players must choose their team, formation and character (and eventually vote for their officers) in that duration.")]
		public int TimeBeforeSpawn = 30;
		[ConfigProperty(label: "Default spawn (attacker)", tooltip: "By default, use spawn positions with this tag for attacker", category: "Spawn location")]
		public string DefaultSpawnTagAttacker = "attacker";
		[ConfigProperty(label: "Default spawn (defender)", tooltip: "By default, use spawn positions with this tag for defender", category: "Spawn location")]
		public string DefaultSpawnTagDefender = "defender";
		[ConfigProperty(label: "Spawn type (attacker)", tooltip: "How to choose the spawn location for the attacker", category: "Spawn location")]
		public LocationStrategy LocationStrategyAttacker = LocationStrategy.OnlyTags;
		[ConfigProperty(label: "Spawn type (defender)", tooltip: "How to choose the spawn location for the defender", category: "Spawn location")]
		public LocationStrategy LocationStrategyDefender = LocationStrategy.OnlyTags;
		[ConfigProperty(label: "Respawn type (attacker)", tooltip: "Define how attackers can respawn", category: "Respawn")]
		public RespawnStrategy RespawnStrategyAttacker = RespawnStrategy.NoRespawn;
		[ConfigProperty(label: "Respawn type (defender)", tooltip: "Define how defenders can respawn", category: "Respawn")]
		public RespawnStrategy RespawnStrategyDefender = RespawnStrategy.NoRespawn;
		[ConfigProperty(label: "Number of lives (attacker)", tooltip: "Number of lives for attacker", category: "Respawn")]
		public int MaxLivesAttacker = 0;
		[ConfigProperty(label: "Number of lives (defender)", tooltip: "Number of lives for defender", category: "Respawn")]
		public int MaxLivesDefender = 0;
		[ConfigProperty(label: "Time before respawn", tooltip: "Time in seconds before a player can respawn after death.", category: "Respawn")]
		public int TimeBeforeRespawn = 10;
		[ConfigProperty(label: "Keep lives from previous act", tooltip: "If enabled, add number of lives on top of the lives left from previous act. Otherwise previous lives left are lost.", category: "Respawn")]
		public bool KeepLivesFromPreviousAct = false;
		
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
		public BasicCharacterObject DefaultCharacterObjectAttacker => Characters.Instance.GetCharacterObject(DefaultCharacterAttacker);
		
		[XmlIgnore]
		public BasicCharacterObject DefaultCharacterObjectDefender => Characters.Instance.GetCharacterObject(DefaultCharacterDefender);

		[XmlIgnore]
		public BasicCharacterObject[] DefaultCharacterObjects => new[] { DefaultCharacterObjectDefender, DefaultCharacterObjectAttacker };

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

		public SpawnLogic(string[] defaultCharacters, string[] spawnTags, int[] maxLives, bool keepLivesFromPreviousAct, LocationStrategy[] locationStrategies, RespawnStrategy[] respawnStrategies)
		{
			DefaultCharacters = defaultCharacters;
			DefaultSpawnTags = spawnTags;
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

	public enum OfficerSelectionStrategy
	{
		NoOfficer,
		RandomOfficer,
		PlayerVote,
	}
}
