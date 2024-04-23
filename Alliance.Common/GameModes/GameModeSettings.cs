using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.SceneList;
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
            ModOptions = ConfigManager.Instance.GetModOptionsCopy();
        }

        /// <summary>
        /// Return list of available Maps for this game mode.
        /// </summary>
        public virtual List<SceneInfo> GetAvailableMaps()
        {
            return Scenes.Where(scene => scene.HasGenericSpawn && !InvalidMaps.Contains(scene.Name)).ToList();
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
            return typeof(Config)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(field => field.Name)
            .ToList();
        }
    }
}