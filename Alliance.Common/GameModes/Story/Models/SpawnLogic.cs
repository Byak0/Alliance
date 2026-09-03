using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story.Attributes;
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
	/// <summary>How players get their team: team selection menu, or auto-assigned by the server.</summary>
	public enum TeamAssignmentMode
	{
		PlayerSelection,
		Auto
	}

	/// <summary>Auto-assignment rule used when TeamAssignment = Auto.</summary>
	public enum TeamAutoAssignmentRule
	{
		Balanced,
		AllAttackers,
		AllDefenders
	}

	/// <summary>How players get their character: spawn menu selection, or auto-assigned by the server.</summary>
	public enum CharacterAssignmentMode
	{
		PlayerSelection,
		Auto
	}

	/// <summary>Auto-assignment rule used when CharacterAssignment = Auto.</summary>
	public enum CharacterAutoAssignmentRule
	{
		DefaultUnits,
		RandomFromMenu
	}

	public class SpawnLogic
	{
		// ----- Team assignment -----
		[ConfigProperty(label: "Team assignment", tooltip: "Players choose their team in the team selection flow, or the server assigns teams automatically at spawn start.")]
		public TeamAssignmentMode TeamAssignment = TeamAssignmentMode.PlayerSelection;

		[ConfigProperty(label: "Auto rule", tooltip: "Balanced = even out both sides. AllAttackers/AllDefenders = everyone on that side.", dependency: $"?{nameof(TeamAssignment)}={nameof(TeamAssignmentMode.Auto)}")]
		public TeamAutoAssignmentRule TeamAutoRule = TeamAutoAssignmentRule.Balanced;

		// ----- Character assignment -----
		[ConfigProperty(label: "Character assignment", tooltip: "Players pick their formation/character in the player spawn menu, or the server assigns characters automatically (no menus).")]
		public CharacterAssignmentMode CharacterAssignment = CharacterAssignmentMode.PlayerSelection;

		[ConfigProperty(label: "Auto rule", tooltip: "DefaultUnits = spawn the default units below. RandomFromMenu = pick a random character from the spawn menu pool of the player's side (menu must be defined).", dependency: $"?{nameof(CharacterAssignment)}={nameof(CharacterAssignmentMode.Auto)}")]
		public CharacterAutoAssignmentRule CharacterAutoRule = CharacterAutoAssignmentRule.DefaultUnits;

		[ConfigProperty(label: "Default unit (attacker)", tooltip: "Character spawned for attackers when auto-assigned.", dataType: AllianceData.DataTypes.Character, dependency: $"?{nameof(CharacterAssignment)}={nameof(CharacterAssignmentMode.Auto)}&?{nameof(CharacterAutoRule)}={nameof(CharacterAutoAssignmentRule.DefaultUnits)}")]
		public ValueSource<string> DefaultCharacterAttacker = new LiteralValue<string>("mp_heavy_infantry_empire_hero");

		[ConfigProperty(label: "Default unit (defender)", tooltip: "Character spawned for defenders when auto-assigned.", dataType: AllianceData.DataTypes.Character, dependency: $"?{nameof(CharacterAssignment)}={nameof(CharacterAssignmentMode.Auto)}&?{nameof(CharacterAutoRule)}={nameof(CharacterAutoAssignmentRule.DefaultUnits)}")]
		public ValueSource<string> DefaultCharacterDefender = new LiteralValue<string>("mp_heavy_infantry_vlandia_hero");

		[ConfigProperty(label: "Player spawn menu", tooltip: "The spawn menu (teams, formations, available characters). Used when Character assignment = Player selection, and as the character pool for the RandomFromMenu auto rule.")]
		public PlayerSpawnMenu PlayerSpawnMenu = new PlayerSpawnMenu();

		[ConfigProperty(label: "Officer selection", tooltip: "How the formation officer is chosen. Requires officer characters in the spawn menu.", dependency: $"?{nameof(CharacterAssignment)}={nameof(CharacterAssignmentMode.PlayerSelection)}")]
		public OfficerSelectionStrategy OfficerSelection = OfficerSelectionStrategy.PlayerVote;

		// ----- Start flow -----
		[ConfigProperty(label: "Intro cinematic", tooltip: "Cinematic defined on this scenario, played when the act starts (after teams and characters are set and agents spawned). Leave empty for none.")]
		[CinematicRef]
		public string IntroCinematicName = "";

		[ConfigProperty(label: "Time before initial spawn", tooltip: "Time in seconds before the initial spawn. In Player selection modes, this is the window players have to choose (and vote for officers).")]
		public ValueSource<int> TimeBeforeSpawn = new LiteralValue<int>(30);

		[ConfigProperty(label: "Time when spawn is allowed", tooltip: "Time in seconds during which new players are allowed to spawn.")]
		public ValueSource<int> TimeWhenSpawnIsAllowed = new LiteralValue<int>(120);

		// Initial spawn settings (attacker)
		[ConfigProperty(label: "Spawn location (attacker)", tooltip: "Where the attacker spawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Initial spawn locations")]
		public LocationStrategy LocationStrategyAttacker = LocationStrategy.Spawnpoint;
		
		[ConfigProperty(label: "Spawnpoint tag (attacker)", tooltip: "Use spawn positions with this tag for attacker", category: "Initial spawn locations", dependency: $"?{nameof(LocationStrategyAttacker)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> SpawnTagAttacker = new LiteralValue<string>("attacker");
		
		[ConfigProperty(label: "Spawn zones (attacker)", tooltip: "List of spawn zones for attacker", category: "Initial spawn locations", dependency: $"?{nameof(LocationStrategyAttacker)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> AttackerSpawnZones = new List<ValueSource<Zone>>();
		
		// Initial spawn settings (defender)
		[ConfigProperty(label: "Spawn location (defender)", tooltip: "Where the defender spawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Initial spawn locations")]
		public LocationStrategy LocationStrategyDefender = LocationStrategy.Spawnpoint;
		
		[ConfigProperty(label: "Spawnpoint tag (defender)", tooltip: "Use spawn positions with this tag for defender", category: "Initial spawn locations", dependency: $"?{nameof(LocationStrategyDefender)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> SpawnTagDefender = new LiteralValue<string>("defender");
		
		[ConfigProperty(label: "Spawn zones (defender)", tooltip: "List of spawn zones for defender", category: "Initial spawn locations", dependency: $"?{nameof(LocationStrategyDefender)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> DefenderSpawnZones = new List<ValueSource<Zone>>();

		// General respawn settings
		[ConfigProperty(label: "Time before respawn", tooltip: "Time in seconds before a player can respawn after death.", category: "Respawn")]
		public ValueSource<int> TimeBeforeRespawn = new LiteralValue<int>(10);

		[ConfigProperty(label: "Keep lives from before", tooltip: "If enabled, add number of lives on top of the lives left from previous act. Otherwise previous lives left are lost.", category: "Respawn")]
		public ValueSource<bool> KeepLivesFromPreviousAct = new LiteralValue<bool>(false);

		// Respawn settings (attacker)
		[ConfigProperty(label: "Respawn type (attacker)", tooltip: "Define how attackers can respawn", category: "Respawn")]
		public RespawnStrategy RespawnStrategyAttacker = RespawnStrategy.NoRespawn;

		[ConfigProperty(label: "Respawn location (attacker)", tooltip: "Where the attacker respawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Respawn", dependency: $"?{nameof(RespawnStrategyAttacker)}!={nameof(RespawnStrategy.NoRespawn)}")]
		public LocationStrategy RespawnLocationStrategyAttacker = LocationStrategy.Spawnpoint;

		[ConfigProperty(label: "Respawn tag (attacker)", tooltip: "Use respawn positions with this tag for attacker", category: "Respawn", dependency: $"?{nameof(RespawnStrategyAttacker)}!={nameof(RespawnStrategy.NoRespawn)}&?{nameof(RespawnLocationStrategyAttacker)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> RespawnTagAttacker = new LiteralValue<string>("attacker");

		[ConfigProperty(label: "Respawn zones (attacker)", tooltip: "List of respawn zones for attacker", category: "Respawn", dependency: $"?{nameof(RespawnStrategyAttacker)}!={nameof(RespawnStrategy.NoRespawn)}&?{nameof(RespawnLocationStrategyAttacker)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> AttackerRespawnZones = new List<ValueSource<Zone>>();

		[ConfigProperty(label: "Number of lives (attacker)", tooltip: "Number of lives for attacker", category: "Respawn", dependency: $"?{nameof(RespawnStrategyAttacker)}!={nameof(RespawnStrategy.NoRespawn)}")]
		public ValueSource<int> MaxLivesAttacker = new LiteralValue<int>(0);

		// Respawn settings (defender)
		[ConfigProperty(label: "Respawn type (defender)", tooltip: "Define how defenders can respawn", category: "Respawn")]
		public RespawnStrategy RespawnStrategyDefender = RespawnStrategy.NoRespawn;

		[ConfigProperty(label: "Respawn location (defender)", tooltip: "Where the defender respawn. Spawnpoint will use a random matching spawnpoint entity. Banner will spawn on an allied banner. Zones will use a random defined zone.", category: "Respawn", dependency: $"?{nameof(RespawnStrategyDefender)}!={nameof(RespawnStrategy.NoRespawn)}")]
		public LocationStrategy RespawnLocationStrategyDefender = LocationStrategy.Spawnpoint;

		[ConfigProperty(label: "Respawn tag (defender)", tooltip: "Use respawn positions with this tag for defender", category: "Respawn", dependency: $"?{nameof(RespawnStrategyDefender)}!={nameof(RespawnStrategy.NoRespawn)}&?{nameof(RespawnLocationStrategyDefender)}={nameof(LocationStrategy.Spawnpoint)}")]
		public ValueSource<string> RespawnTagDefender = new LiteralValue<string>("defender");

		[ConfigProperty(label: "Respawn zones (defender)", tooltip: "List of respawn zones for defender", category: "Respawn", dependency: $"?{nameof(RespawnStrategyDefender)}!={nameof(RespawnStrategy.NoRespawn)}&?{nameof(RespawnLocationStrategyDefender)}={nameof(LocationStrategy.Zones)}")]
		public List<ValueSource<Zone>> DefenderRespawnZones = new List<ValueSource<Zone>>();

		[ConfigProperty(label: "Number of lives (defender)", tooltip: "Number of lives for defender", category: "Respawn", dependency: $"?{nameof(RespawnStrategyDefender)}!={nameof(RespawnStrategy.NoRespawn)}")]
		public ValueSource<int> MaxLivesDefender = new LiteralValue<int>(0);

		// Runtime properties
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