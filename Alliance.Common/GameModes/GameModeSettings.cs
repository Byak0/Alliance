using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Utilities;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes
{
    /// <summary>
    /// Store Game Mode informations and list of options.
    /// </summary>
    public class GameModeSettings
    {
        public readonly string GameMode;
        public readonly string GameModeName;
        public readonly string GameModeDescription;

        public List<MultiplayerOption> NativeOptions;
        public Config ModOptions;

        public GameModeSettings(string gameMode, string gameModeName, string gameModeDescription)
        {
            GameMode = gameMode;
            GameModeName = gameModeName;
            GameModeDescription = gameModeDescription;
            SetDefaultNativeOptions();
            SetDefaultModOptions();
        }

        public void SetNativeOption(OptionType optionType, object value)
        {
            MultiplayerOption option = NativeOptions.FirstOrDefault(x => x.OptionType == optionType);
            if (option != null)
            {
                switch (optionType.GetOptionProperty().OptionValueType)
                {
                    case OptionValueType.Bool:
                        option.UpdateValue((bool)value);
                        break;
                    case OptionValueType.Integer:
                    case OptionValueType.Enum:
                        option.UpdateValue((int)value);
                        break;
                    case OptionValueType.String:
                        option.UpdateValue((string)value);
                        break;
                }
            }
        }

        /// <summary>
        /// Set the default native options for this game mode.
        /// </summary>
        public virtual void SetDefaultNativeOptions()
        {
            NativeOptions = ConfigManager.Instance.GetNativeOptionsCopy();
            SetNativeOption(OptionType.GameType, GameMode);
        }

        /// <summary>
        /// Set the default mod options for this game mode.
        /// </summary>
        public virtual void SetDefaultModOptions()
        {
            ModOptions = new Config();
        }

        /// <summary>
        /// Return list of available Maps for this game mode.
        /// </summary>
        public virtual List<string> GetAvailableMaps()
        {
            return SceneList.Scenes
                    .Where(scene => new[] { "character_", "editor_" } // Invalid maps
                    .All(prefix => !scene.Contains(prefix)))
                    .ToList();
        }

        /// <summary>
        /// Return list of available native options for this game mode.
        /// </summary>
        public virtual List<OptionType> GetAvailableNativeOptions()
        {
            return new List<OptionType>
            {
                OptionType.CultureTeam1,
                OptionType.CultureTeam2,
                OptionType.NumberOfBotsTeam1,
                OptionType.NumberOfBotsTeam2
            };
        }

        /// <summary>
        /// Return list of available mod options for this game mode.
        /// </summary>
        public virtual List<string> GetAvailableModOptions()
        {
            return new List<string>
            {
                nameof(Config.ActivateSAE),
                nameof(Config.SAERange),
                nameof(Config.AllowSpawnInRound),
                nameof(Config.FreeRespawnTimer),
                nameof(Config.AllowCustomBody),
                nameof(Config.RandomizeAppearance),
                nameof(Config.ShowFlagMarkers),
                nameof(Config.ShowScore),
                nameof(Config.ShowOfficers),
                nameof(Config.ShowWeaponTrail),
                nameof(Config.KillFeedEnabled),
                nameof(Config.UseTroopCost),
                nameof(Config.StartingGold),
                nameof(Config.MinTroopCost),
                nameof(Config.MaxTroopCost),
                nameof(Config.GoldPerKill),
                nameof(Config.GoldPerAssist),
                nameof(Config.GoldPerAllyDead),
                nameof(Config.UseTroopLimit),
                nameof(Config.CommanderSide),
                nameof(Config.MinPlayer),
                nameof(Config.MaxPlayer),
                nameof(Config.FormRadMin),
                nameof(Config.FormRadMax),
                nameof(Config.SkirmRadMin),
                nameof(Config.SkirmRadMax),
                nameof(Config.NbFormMin),
                nameof(Config.NbFormMax),
                nameof(Config.NbSkirmMin),
                nameof(Config.NbSkirmMax),
                nameof(Config.MinPlayerForm),
                nameof(Config.MeleeDebuffRambo),
                nameof(Config.DistDebuffRambo),
                nameof(Config.AccDebuffRambo),
                nameof(Config.MeleeDebuffSkirm),
                nameof(Config.DistDebuffSkirm),
                nameof(Config.AccDebuffSkirm),
                nameof(Config.OfficerHPMultip)
            };
        }
    }
}