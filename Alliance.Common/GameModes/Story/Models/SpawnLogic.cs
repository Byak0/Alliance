using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Core;

namespace Alliance.Common.GameModes.Story.Models
{
	public enum LocationStrategy
	{
		Spawnpoint,
		Banners,
		Zones
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

	/// <summary>
	/// Store informations about how, when and where the players and IA must spawn.
	/// </summary>
	[Serializable]
	public class SpawnLogic
	{
		// General spawn settings
		[ConfigProperty(label: "Player spawn menu", tooltip: "Define the player spawn menu for this act. Set teams, formations and available characters.")]
		public PlayerSpawnMenu PlayerSpawnMenu = new PlayerSpawnMenu();
		[ConfigProperty(label: "Default unit (attacker)", tooltip: "Default character to spawn for attacker. Only used if the player spawn menu is undefined.", dataType: AllianceData.DataTypes.Character)]
		public ValueSource<string> DefaultCharacterAttacker = new LiteralValue<string>("mp_heavy_infantry_empire_hero");
		[ConfigProperty(label: "Default unit (defender)", tooltip: "Default character to spawn for defender. Only used if the player spawn menu is undefined.", dataType: AllianceData.DataTypes.Character)]
		public ValueSource<string> DefaultCharacterDefender = new LiteralValue<string>("mp_heavy_infantry_vlandia_hero");
		[ConfigProperty(label: "Officer selection strategy", tooltip: "Define how the officer is selected. You must define officer characters in the player spawn menu.")]
		public OfficerSelectionStrategy OfficerSelection = OfficerSelectionStrategy.PlayerVote;
		[ConfigProperty(label: "Time before initial spawn", tooltip: "Time in seconds before the original spawn. Players must choose their team, formation and character (and eventually vote for their officers) in that duration.")]
		public ValueSource<int> TimeBeforeSpawn = new LiteralValue<int>(30);
		[ConfigProperty(label: "Time when spawn is allowed", tooltip: "Time in seconds during which new players are allowed to spawn.")]
		public ValueSource<int> TimeWhenSpawnIsAllowed = new LiteralValue<int>(120);

		// Initial spawn settings (attacker)
		[ConfigProperty(label: "Spawn location (attacker)", tooltip: "Where the attacker spawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Initial spawn (attacker)")]
		public LocationStrategy LocationStrategyAttacker = LocationStrategy.Spawnpoint;
		[ConfigProperty(label: "Spawnpoint tag (attacker)", tooltip: "Use spawn positions with this tag for attacker", category: "Initial spawn (attacker)", dependency: $"?{nameof(LocationStrategyAttacker)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> SpawnTagAttacker = new LiteralValue<string>("attacker");
		[ConfigProperty(label: "Spawn zones (attacker)", tooltip: "List of spawn zones for attacker", category: "Initial spawn (attacker)", dependency: $"?{nameof(LocationStrategyAttacker)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> AttackerSpawnZones = new List<ValueSource<Zone>>();
		
		// Initial spawn settings (defender)
		[ConfigProperty(label: "Spawn location (defender)", tooltip: "Where the defender spawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Initial spawn (defender)")]
		public LocationStrategy LocationStrategyDefender = LocationStrategy.Spawnpoint;
		[ConfigProperty(label: "Spawnpoint tag (defender)", tooltip: "Use spawn positions with this tag for defender", category: "Initial spawn (defender)", dependency: $"?{nameof(LocationStrategyDefender)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> SpawnTagDefender = new LiteralValue<string>("defender");
		[ConfigProperty(label: "Spawn zones (defender)", tooltip: "List of spawn zones for defender", category: "Initial spawn (defender)", dependency: $"?{nameof(LocationStrategyDefender)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> DefenderSpawnZones = new List<ValueSource<Zone>>();

		// General respawn settings
		[ConfigProperty(label: "Time before respawn", tooltip: "Time in seconds before a player can respawn after death.")]
		public ValueSource<int> TimeBeforeRespawn = new LiteralValue<int>(10);
		[ConfigProperty(label: "Keep lives from previous act", tooltip: "If enabled, add number of lives on top of the lives left from previous act. Otherwise previous lives left are lost.")]
		public ValueSource<bool> KeepLivesFromPreviousAct = new LiteralValue<bool>(false);

		// Respawn settings (attacker)
		[ConfigProperty(label: "Respawn type (attacker)", tooltip: "Define how attackers can respawn", category: "Respawn (attacker)")]
		public RespawnStrategy RespawnStrategyAttacker = RespawnStrategy.NoRespawn;
		[ConfigProperty(label: "Respawn location (attacker)", tooltip: "Where the attacker spawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Respawn (attacker)")]
		public LocationStrategy RespawnLocationStrategyAttacker = LocationStrategy.Spawnpoint;
		[ConfigProperty(label: "Respawn tag (attacker)", tooltip: "Use spawn positions with this tag for attacker", category: "Respawn (attacker)", dependency: $"?{nameof(RespawnLocationStrategyAttacker)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> RespawnTagAttacker = new LiteralValue<string>("attacker");
		[ConfigProperty(label: "Respawn zones (attacker)", tooltip: "List of respawn zones for attacker", category: "Respawn (attacker)", dependency: $"?{nameof(RespawnLocationStrategyAttacker)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> AttackerRespawnZones = new List<ValueSource<Zone>>();
		[ConfigProperty(label: "Number of lives (attacker)", tooltip: "Number of lives for attacker", category: "Respawn (attacker)")]
		public ValueSource<int> MaxLivesAttacker = new LiteralValue<int>(0);

		// Respawn settings (defender)
		[ConfigProperty(label: "Respawn type (defender)", tooltip: "Define how defenders can respawn", category: "Respawn (defender)")]
		public RespawnStrategy RespawnStrategyDefender = RespawnStrategy.NoRespawn;
		[ConfigProperty(label: "Respawn location (defender)", tooltip: "Where the defender spawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Respawn (defender)")]
		public LocationStrategy RespawnLocationStrategyDefender = LocationStrategy.Spawnpoint;
		[ConfigProperty(label: "Respawn tag (defender)", tooltip: "Use spawn positions with this tag for defender", category: "Respawn (defender)", dependency: $"?{nameof(RespawnLocationStrategyDefender)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> RespawnTagDefender = new LiteralValue<string>("defender");
		[ConfigProperty(label: "Respawn zones (defender)", tooltip: "List of respawn zones for defender", category: "Respawn (defender)", dependency: $"?{nameof(RespawnLocationStrategyDefender)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> DefenderRespawnZones = new List<ValueSource<Zone>>();
		[ConfigProperty(label: "Number of lives (defender)", tooltip: "Number of lives for defender", category: "Respawn (defender)")]
		public ValueSource<int> MaxLivesDefender = new LiteralValue<int>(0);
		
		[XmlIgnore]
		public BasicCharacterObject DefaultCharacterObjectAttacker => Characters.Instance.GetCharacterObject(DefaultCharacterAttacker.Resolve());
		
		[XmlIgnore]
		public BasicCharacterObject DefaultCharacterObjectDefender => Characters.Instance.GetCharacterObject(DefaultCharacterDefender.Resolve());

		[XmlIgnore]
		public BasicCharacterObject[] DefaultCharacterObjects => new[] { DefaultCharacterObjectDefender, DefaultCharacterObjectAttacker };

		[XmlIgnore]
		public string[] DefaultSpawnTags
		{
			get => new[] { SpawnTagDefender.Resolve(), SpawnTagAttacker.Resolve() };
			set
			{
				if (value.Length >= 2)
				{
					SpawnTagDefender = new LiteralValue<string>(value[0]);
					SpawnTagAttacker = new LiteralValue<string>(value[1]);
				}
			}
		}

		[XmlIgnore]
		public int[] MaxLives
		{
			get => new[] { MaxLivesDefender.Resolve(), MaxLivesAttacker.Resolve() };
			set
			{
				if (value.Length >= 2)
				{
					MaxLivesDefender = new LiteralValue<int>(value[0]);
					MaxLivesAttacker = new LiteralValue<int>(value[1]);
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
			DefaultSpawnTags = spawnTags;
			MaxLives = maxLives;
			KeepLivesFromPreviousAct = new LiteralValue<bool>(keepLivesFromPreviousAct);
			LocationStrategies = locationStrategies;
			RespawnStrategies = respawnStrategies;
		}

		public SpawnLogic() { }
	}
}
