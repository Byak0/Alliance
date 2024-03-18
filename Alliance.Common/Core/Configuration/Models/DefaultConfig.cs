using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using static Alliance.Common.Extensions.TroopSpawner.Utilities.SpawnHelper;

namespace Alliance.Common.Core.Configuration.Models
{
    [Serializable]
    public class DefaultConfig
    {
        /// <summary>
        /// List available values for the enum options
        /// </summary>
        public static List<string> GetAvailableValuesForOption(FieldInfo option)
        {
            List<string> values = new List<string>();
            switch (option.Name)
            {
                case nameof(TestEnum):
                    values = new List<string>() { "Test", "Test1" };
                    break;
                case nameof(BotDifficulty):
                    values = new List<string>() {
                        nameof(Difficulty.PlayerChoice),
                        nameof(Difficulty.Easy),
                        nameof(Difficulty.Normal),
                        nameof(Difficulty.Hard),
                        nameof(Difficulty.VeryHard),
                        nameof(Difficulty.Bannerlord)
                    };
                    break;
                default:
                    break;
            }

            return values;
        }

        [ConfigProperty("Synchronize configuration", "Synchronize server configuration with the clients.", ConfigValueType.Bool)]
        public bool SyncConfig = true;

        [ConfigProperty("Toggle bot talks", "Bots will repeat what players say. For when you got no friend.", ConfigValueType.Bool)]
        public bool NoFriend = false;

        [ConfigProperty("Test enum", "Test enum", ConfigValueType.Enum)]
        public string TestEnum = "Test";

        [ConfigProperty("Bot difficulty", "Choose how good the bots are in combat. Set to PlayerChoice to allow custom difficulty when recruiting.", ConfigValueType.Enum)]
        public string BotDifficulty = nameof(Difficulty.Normal);

        [ConfigProperty("Toggle SAE", "Activate or not Scatter Around Expanded mod.", ConfigValueType.Bool)]
        public bool ActivateSAE = true;
        [ConfigProperty("SAE range", "Indicate the max distance where a troop need to be in order to go to one marker.", ConfigValueType.Integer, 0, 1000)]
        public int SAERange = 30;
        [ConfigProperty("Toggle formation", "Activate or not the formation system (debuff isolated players).", ConfigValueType.Bool)]
        public bool EnableFormation = true;

        [ConfigProperty("Use gold to recruit troops", "Use gold system to recruit troops.", ConfigValueType.Bool)]
        public bool UseTroopCost = false;
        [ConfigProperty("Gold multiplier", "Gold multiplier when giving gold to commander (ennemy army value X multiplier).", ConfigValueType.Float, 0f, 5f)]
        public float GoldMultiplier = 1f;
        [ConfigProperty("Commander starting gold", "Starting gold for commander.", ConfigValueType.Integer, 0, 10000)]
        public int StartingGold = 5000;

        [ConfigProperty("Zone life time", "Number of seconds the zone takes to reach its minimum size.", ConfigValueType.Integer, 0, 3600)]
        public int BRZoneLifeTime = 300;

        [ConfigProperty("Allow spawn during round", "Allow players to spawn while round is in progress.", ConfigValueType.Bool)]
        public bool AllowSpawnInRound = true;
        [ConfigProperty("Free respawn timer", "Duration in seconds during which players can freely respawn after round start.", ConfigValueType.Integer, 0, 3600)]
        public int FreeRespawnTimer = 60;

        [ConfigProperty("Allow custom appearance", "Spawn players with their custom appearance instead of the character default one.", ConfigValueType.Bool)]
        public bool AllowCustomBody = false;
        [ConfigProperty("Randomize bot appearance", "Randomize bots appearance. If false, all bots will have the same appearance.", ConfigValueType.Bool)]
        public bool RandomizeAppearance = true;

        [ConfigProperty("Show flag markers", "Show flags markers when pressing alt.", ConfigValueType.Bool)]
        public bool ShowFlagMarkers = true;
        [ConfigProperty("Show score", "Enable score UI on top of the screen.", ConfigValueType.Bool)]
        public bool ShowScore = true;
        [ConfigProperty("Show officers names", "Show officers names on top of the screen.", ConfigValueType.Bool)]
        public bool ShowOfficers = true;
        [ConfigProperty("Show projectile trail", "Show trail behind projectile.", ConfigValueType.Bool)]
        public bool ShowWeaponTrail = true;
        [ConfigProperty("Enable Kill Feed", "Enable the kill feed on top right of the screen.", ConfigValueType.Bool)]
        public bool KillFeedEnabled = true;

        [ConfigProperty("Time before flags removal", "Number of seconds before the removal of flags.", ConfigValueType.Integer, 0, 3600)]
        public int TimeBeforeFlagRemoval = 300;
        [ConfigProperty("Morale multiplier for flag", "More gain/loss is multiplied by this amount for each flag controlled.", ConfigValueType.Float, 0, 10f)]
        public float MoraleMultiplierForFlag = 1f;
        [ConfigProperty("Morale multiplier for last flag", "More gain/loss is multiplied by this amount when last flag is controlled.", ConfigValueType.Float, 0, 10f)]
        public float MoraleMultiplierForLastFlag = 1f;

        [ConfigProperty("Minimum troop cost", "Minimum cost for troops. Unit cost will be automatically calculated between min and max cost.", ConfigValueType.Integer, 0, 200)]
        public int MinTroopCost = 10;
        [ConfigProperty("Maximum troop cost", "Maximum cost for troops. Unit cost will be automatically calculated between min and max cost.", ConfigValueType.Integer, 0, 200)]
        public int MaxTroopCost = 50;
        [ConfigProperty("Gold on kill", "Gold gained on kill.", ConfigValueType.Integer, 0, 200)]
        public int GoldPerKill = 10;
        [ConfigProperty("Gold on assist", "Gold gained on assist.", ConfigValueType.Integer, 0, 200)]
        public int GoldPerAssist = 5;
        [ConfigProperty("Gold on lost ally", "Gold gained when an ally died.", ConfigValueType.Integer, 0, 200)]
        public int GoldPerAllyDead = 5;

        [ConfigProperty("Enable troop limits", "Limit the number of troops a commander can recruit. Limits are defined in ExtendedCharacters.xml.", ConfigValueType.Bool)]
        public bool UseTroopLimit = false;

        [ConfigProperty("Commander side", "Commander side for Alliance. 0 = defender, 1 = attacker.", ConfigValueType.Integer, 0, 1)]
        public int CommanderSide = (int)BattleSideEnum.Defender;

        [ConfigProperty("Formation scaling - min players", "Formation values (radius, required number for a formation) will scale depending on troops number (limited to min and max).", ConfigValueType.Integer, 0, 200)]
        public int MinPlayer = 10;
        [ConfigProperty("Formation scaling - max players", "Formation values (radius, required number for a formation) will scale depending on troops number (limited to min and max).", ConfigValueType.Integer, 0, 200)]
        public int MaxPlayer = 120;
        [ConfigProperty("Formation - min radius", "Minimum radius for formation check. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int FormRadMin = 2;
        [ConfigProperty("Formation - max radius", "Maximum radius for formation check. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int FormRadMax = 4;
        [ConfigProperty("Skirmish - min radius", "Minimum radius for skirmish check. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int SkirmRadMin = 10;
        [ConfigProperty("Skirmish - max radius", "Maximum radius for skirmish check. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int SkirmRadMax = 20;
        [ConfigProperty("Formation - min troops", "Minimum number of troops to be in formation state. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int NbFormMin = 2;
        [ConfigProperty("Formation - max troops", "Maximum number of troops to be in formation state. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int NbFormMax = 8;
        [ConfigProperty("Skirmish - min troops", "Minimum number of troops to be in skirmish state. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int NbSkirmMin = 2;
        [ConfigProperty("Skirmish - max troops", "Maximum number of troops to be in skirmish state. Scale with troops number.", ConfigValueType.Integer, 0, 50)]
        public int NbSkirmMax = 4;
        [ConfigProperty("Minimum alive troops for formation", "Minimum number of troops alive for the formation system to be enabled.", ConfigValueType.Integer, 0, 200)]
        public int MinPlayerForm = 15;

        [ConfigProperty("Melee debuff for rambo", "Melee debuff for people in rambo state. Range from 0 (hardest) to 1 (no effect).", ConfigValueType.Float, 0f, 1f)]
        public float MeleeDebuffRambo = 0.6f;
        [ConfigProperty("Distance debuff for rambo", "Distance debuff for people in rambo state. Range from 0 (hardest) to 1 (no effect).", ConfigValueType.Float, 0f, 1f)]
        public float DistDebuffRambo = 0.8f;
        [ConfigProperty("Accuracy debuff for rambo", "Accuracy debuff for people in rambo state. Range from 0 (hardest) to 1 (no effect).", ConfigValueType.Float, 0f, 1f)]
        public float AccDebuffRambo = 0.9f;
        [ConfigProperty("Melee debuff for skirmish", "Melee debuff for people in skirmish state. Range from 0 (hardest) to 1 (no effect).", ConfigValueType.Float, 0f, 1f)]
        public float MeleeDebuffSkirm = 0.8f;
        [ConfigProperty("Distance debuff for skirmish", "Distance debuff for people in skirmish state. Range from 0 (hardest) to 1 (no effect).", ConfigValueType.Float, 0f, 1f)]
        public float DistDebuffSkirm = 0.98f;
        [ConfigProperty("Accuracy debuff for skirmish", "Accuracy debuff for people in skirmish state. Range from 0 (hardest) to 1 (no effect).", ConfigValueType.Float, 0f, 1f)]
        public float AccDebuffSkirm = 0.96f;
        [ConfigProperty("Melee debuff for formation", "Melee debuff for people in formation state. Range from 0 (hardest) to 1 (no effect) to 2 (giving buff).", ConfigValueType.Float, 0f, 2f)]
        public float MeleeDebuffForm = 1f;
        [ConfigProperty("Distance debuff for formation", "Distance debuff for people in formation state. Range from 0 (hardest) to 1 (no effect) to 2 (giving buff).", ConfigValueType.Float, 0f, 2f)]
        public float DistDebuffForm = 1.25f;
        [ConfigProperty("Accuracy debuff for formation", "Accuracy debuff for people in formation state. Range from 0 (hardest) to 1 (no effect) to 2 (giving buff).", ConfigValueType.Float, 0f, 2f)]
        public float AccDebuffForm = 1f;

        [ConfigProperty("HP multiplier for officers", "Health is multiplied by this value for officers.", ConfigValueType.Float, 0f, 10f)]
        public float OfficerHPMultip = 1f;

        public DefaultConfig()
        {
        }
    }
}