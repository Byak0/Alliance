using System;
using TaleWorlds.Core;
using static Alliance.Common.Core.Configuration.Models.AllianceData;

namespace Alliance.Common.Core.Configuration.Models
{
	[Serializable]
	public class DefaultConfig
	{
		[ConfigProperty(false, "Synchronize configuration", "Synchronize server configuration with the clients.")]
		public bool SyncConfig = true;

		[ConfigProperty(true, "Toggle bot talks", "Bots will repeat what players say. For when you got no friend.")]
		public bool NoFriend = false;

		[ConfigProperty(true, "Authorize Poll", "Authorize everyone to use the GameMode menu when in Lobby.")]
		public bool AuthorizePoll = true;

		[ConfigProperty(true, "Bot difficulty", "Choose how good the bots are in combat. Set to PlayerChoice to allow custom difficulty when recruiting.", dataType: DataTypes.Difficulty)]
		public string BotDifficulty = nameof(Difficulty.Normal);
		[ConfigProperty(true, "Show difficulty slider", "Show the difficulty slider in the troop menu. If false, the difficulty will be set to BotDifficulty value.")]
		public bool ShowDifficultySlider = true;

		[ConfigProperty(true, "Toggle SAE", "Activate or not Scatter Around Expanded mod.")]
		public bool ActivateSAE = true;
		[ConfigProperty(true, "SAE range", "Indicate the max distance where a troop need to be in order to go to one marker.", 0, 1000)]
		public int SAERange = 30;
		[ConfigProperty(true, "Toggle formation", "Activate or not the formation system (debuff isolated players).")]
		public bool EnableFormation = false;
		[ConfigProperty(true, "Enable player limits", "Limit the number of characters available to players. Limits are defined in CharactersExtended.xml.")]
		public bool UsePlayerLimit = true;

		[ConfigProperty(true, "Use gold to recruit troops", "Use gold system to recruit troops.")]
		public bool UseTroopCost = false;
		[ConfigProperty(true, "Gold multiplier", "Gold multiplier when giving gold to commander (ennemy army value X multiplier).", 0f, 5f)]
		public float GoldMultiplier = 1f;
		[ConfigProperty(true, "Commander starting gold", "Starting gold for commander.", 0, 10000)]
		public int StartingGold = 5000;

		[ConfigProperty(true, "Zone life time", "Number of seconds the zone takes to reach its minimum size.", 0, 3600)]
		public int BRZoneLifeTime = 300;

		[ConfigProperty(true, "Time before start", "Duration in seconds during which players are stuck in starting zone.", 0, 600)]
		public int TimeBeforeStart = 30;

		[ConfigProperty(true, "Allow spawn during round", "Allow players to spawn while round is in progress.")]
		public bool AllowSpawnInRound = true;
		[ConfigProperty(true, "Free respawn timer", "Duration in seconds during which players can freely respawn after round start.", 0, 3600)]
		public int FreeRespawnTimer = 60;

		[ConfigProperty(true, "Allow custom appearance", "Spawn players with their custom appearance instead of the character default one.")]
		public bool AllowCustomBody = false;
		[ConfigProperty(true, "Randomize bot appearance", "Randomize bots appearance. If false, all bots will have the same appearance.")]
		public bool RandomizeAppearance = true;

		[ConfigProperty(true, "Enable Race Restriction On Stuff", "Stuff belonging to a race cannot be pickup by other race.")]
		public bool EnableRaceRestrictionOnStuff = true;

		[ConfigProperty(true, "Show flag markers", "Show flags markers when pressing alt.")]
		public bool ShowFlagMarkers = true;
		[ConfigProperty(true, "Show score", "Enable score UI on top of the screen.")]
		public bool ShowScore = true;
		[ConfigProperty(true, "Show officers names", "Show officers names on top of the screen.")]
		public bool ShowOfficers = true;
		[ConfigProperty(true, "Show projectile trail", "Show trail behind projectile.")]
		public bool ShowWeaponTrail = false;
		[ConfigProperty(true, "Enable Kill Feed", "Enable the kill feed on top right of the screen.")]
		public bool KillFeedEnabled = true;

		[ConfigProperty(true, "Time before flags removal", "Number of seconds before the removal of flags.", 0, 3600)]
		public int TimeBeforeFlagRemoval = 300;
		[ConfigProperty(true, "Morale multiplier for flag", "More gain/loss is multiplied by this amount for each flag controlled.", 0, 10f)]
		public float MoraleMultiplierForFlag = 1f;
		[ConfigProperty(true, "Morale multiplier for last flag", "More gain/loss is multiplied by this amount when last flag is controlled.", 0, 10f)]
		public float MoraleMultiplierForLastFlag = 1f;

		[ConfigProperty(true, "Minimum troop cost", "Minimum cost for troops. Unit cost will be automatically calculated between min and max cost.", 0, 200)]
		public int MinTroopCost = 10;
		[ConfigProperty(true, "Maximum troop cost", "Maximum cost for troops. Unit cost will be automatically calculated between min and max cost.", 0, 200)]
		public int MaxTroopCost = 50;
		[ConfigProperty(true, "Gold on kill", "Gold gained on kill.", 0, 200)]
		public int GoldPerKill = 10;
		[ConfigProperty(true, "Gold on assist", "Gold gained on assist.", 0, 200)]
		public int GoldPerAssist = 5;
		[ConfigProperty(true, "Gold on lost ally", "Gold gained when an ally died.", 0, 200)]
		public int GoldPerAllyDead = 5;

		[ConfigProperty(true, "Enable troop limits", "Limit the number of troops a commander can recruit. Limits are defined in CharactersExtended.xml.")]
		public bool UseTroopLimit = false;

		[ConfigProperty(true, "Commander side", "Commander side for Alliance.", dataType: DataTypes.BattleSide)]
		public string CommanderSide = nameof(BattleSideEnum.Defender);

		[ConfigProperty(true, "Formation scaling - min players", "Formation values (radius, required number for a formation) will scale depending on troops number (limited to min and max).", 0, 200)]
		public int MinPlayer = 10;
		[ConfigProperty(true, "Formation scaling - max players", "Formation values (radius, required number for a formation) will scale depending on troops number (limited to min and max).", 0, 200)]
		public int MaxPlayer = 120;
		[ConfigProperty(true, "Formation - min radius", "Minimum radius for formation check. Scale with troops number.", 0, 50)]
		public int FormRadMin = 2;
		[ConfigProperty(true, "Formation - max radius", "Maximum radius for formation check. Scale with troops number.", 0, 50)]
		public int FormRadMax = 4;
		[ConfigProperty(true, "Skirmish - min radius", "Minimum radius for skirmish check. Scale with troops number.", 0, 50)]
		public int SkirmRadMin = 10;
		[ConfigProperty(true, "Skirmish - max radius", "Maximum radius for skirmish check. Scale with troops number.", 0, 50)]
		public int SkirmRadMax = 20;
		[ConfigProperty(true, "Formation - min troops", "Minimum number of troops to be in formation state. Scale with troops number.", 0, 50)]
		public int NbFormMin = 2;
		[ConfigProperty(true, "Formation - max troops", "Maximum number of troops to be in formation state. Scale with troops number.", 0, 50)]
		public int NbFormMax = 8;
		[ConfigProperty(true, "Skirmish - min troops", "Minimum number of troops to be in skirmish state. Scale with troops number.", 0, 50)]
		public int NbSkirmMin = 2;
		[ConfigProperty(true, "Skirmish - max troops", "Maximum number of troops to be in skirmish state. Scale with troops number.", 0, 50)]
		public int NbSkirmMax = 4;
		[ConfigProperty(true, "Minimum alive troops for formation", "Minimum number of troops alive for the formation system to be enabled.", 0, 200)]
		public int MinPlayerForm = 15;

		[ConfigProperty(true, "Cavalry formation radius multiplier", "Radius of cavalry formation will be increase by this multiplicator (Base on infantry radius).", 0.1f, 5)]
		public float CavFormationRadiusMultiplier = 2f;
		[ConfigProperty(true, "Archer formation radius multiplier", "Radius of archer formation will be increase by this multiplicator (Base on infantry radius).", 0.1f, 5)]
		public float RangedFormationRadiusMultiplier = 1.7f;
		[ConfigProperty(true, "Cavalry skirmish radius multiplier", "Radius of cavalry skirmish will be increase by this multiplicator (Base on infantry radius).", 0.1f, 5)]
		public float CavSkirmishRadiusMultiplier = 1.5f;
		[ConfigProperty(true, "Archer skirmish radius multiplier", "Radius of archer skirmish will be increase by this multiplicator (Base on infantry radius).", 0.1f, 5)]
		public float RangedSkirmishRadiusMultiplier = 1.2f;

		[ConfigProperty(true, "Melee debuff for rambo", "Melee debuff for people in rambo state. Range from 0 (hardest) to 1 (no effect).", 0f, 1f)]
		public float MeleeDebuffRambo = 0.6f;
		[ConfigProperty(true, "Distance debuff for rambo", "Distance debuff for people in rambo state. Range from 0 (hardest) to 1 (no effect).", 0f, 1f)]
		public float DistDebuffRambo = 0.8f;
		[ConfigProperty(true, "Accuracy debuff for rambo", "Accuracy debuff for people in rambo state. Range from 0 (hardest) to 1 (no effect).", 0f, 1f)]
		public float AccDebuffRambo = 0.9f;
		[ConfigProperty(true, "Melee debuff for skirmish", "Melee debuff for people in skirmish state. Range from 0 (hardest) to 1 (no effect).", 0f, 1f)]
		public float MeleeDebuffSkirm = 0.8f;
		[ConfigProperty(true, "Distance debuff for skirmish", "Distance debuff for people in skirmish state. Range from 0 (hardest) to 1 (no effect).", 0f, 1f)]
		public float DistDebuffSkirm = 0.98f;
		[ConfigProperty(true, "Accuracy debuff for skirmish", "Accuracy debuff for people in skirmish state. Range from 0 (hardest) to 1 (no effect).", 0f, 1f)]
		public float AccDebuffSkirm = 0.96f;
		[ConfigProperty(true, "Melee debuff for formation", "Melee debuff for people in formation state. Range from 0 (hardest) to 1 (no effect) to 2 (giving buff).", 0f, 2f)]
		public float MeleeDebuffForm = 1f;
		[ConfigProperty(true, "Distance debuff for formation", "Distance debuff for people in formation state. Range from 0 (hardest) to 1 (no effect) to 2 (giving buff).", 0f, 2f)]
		public float DistDebuffForm = 1.25f;
		[ConfigProperty(true, "Accuracy debuff for formation", "Accuracy debuff for people in formation state. Range from 0 (hardest) to 1 (no effect) to 2 (giving buff).", 0f, 2f)]
		public float AccDebuffForm = 1f;

		[ConfigProperty(true, "HP multiplier for officers", "Health is multiplied by this value for officers.", 0f, 10f)]
		public float OfficerHPMultip = 1f;

		public DefaultConfig()
		{
		}
	}
}