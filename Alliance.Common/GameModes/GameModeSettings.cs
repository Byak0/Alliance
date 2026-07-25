using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Alliance.Common.Utilities;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using TaleWorlds.ModuleManager;
using static Alliance.Common.Utilities.Logger;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes
{
	/// <summary>
	/// Store Game Mode informations and list of options.
	/// </summary>
	public class GameModeSettings
	{
		[ConfigProperty(isEditable: false)]
		public string GameMode;

		[ConfigProperty(isEditable: false)]
		public string GameModeName;

		[ConfigProperty(isEditable: false)]
		public string GameModeDescription;

		[ConfigProperty(label: "Native options", tooltip: "Native options from TW.")]
		[InlineContent]
		public TWConfig TWOptions;

		[ConfigProperty(label: "Mod options", tooltip: "Additional options from Alliance.")]
		[InlineContent]
		public Config ModOptions;

		public GameModeSettings(string gameMode, string gameModeName, string gameModeDescription)
		{
			GameMode = gameMode;
			GameModeName = gameModeName;
			GameModeDescription = gameModeDescription;
			SetDefaultNativeOptions();
			SetDefaultModOptions();
		}



		public GameModeSettings() { }

		/// <summary>
		/// Set the default native options for this game mode.
		/// </summary>
		public virtual void SetDefaultNativeOptions()
		{
			TWOptions = ConfigManager.Instance.GetNativeOptionsCopy();
			TWOptions[OptionType.GameType] = GameMode;
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
				OptionType.GameType,
				OptionType.CultureTeam1,
				OptionType.CultureTeam2,
				OptionType.NumberOfBotsPerFormation,
				OptionType.NumberOfBotsTeam1,
				OptionType.NumberOfBotsTeam2,
				OptionType.AutoTeamBalanceThreshold,
				OptionType.WarmupTimeLimitInSeconds,
				OptionType.RoundPreparationTimeLimit,
				OptionType.RoundTimeLimit,
				OptionType.RoundTotal,
				OptionType.UnlimitedGold,
				OptionType.FriendlyFireDamageMeleeFriendPercent,
				OptionType.FriendlyFireDamageMeleeSelfPercent,
				OptionType.FriendlyFireDamageRangedFriendPercent,
				OptionType.FriendlyFireDamageRangedSelfPercent
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

		/// <summary>
		/// Try to load the GameModeSettings from file. Returns true if successful, false otherwise.
		/// </summary>
		public static bool TryLoadFromFile(string fileName, out GameModeSettings newSettings)
		{
			newSettings = null;

			// Load the selected file
			string filePath = Path.GetFullPath(Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), "Map_Presets", fileName));
			try
			{
				if (File.Exists(filePath))
				{
					Log($"Loading GameModeSettings from {filePath}");
					newSettings = SerializeHelper.LoadAbstractClassFromFile(filePath, new GameModeSettings());
					return true;
				}
				else
				{
					Log($"Can't load GameModeSettings, file doesn't exist : {filePath}", LogLevel.Error);
				}
			}
			catch (Exception ex)
			{
				Log($"Failed to load GameModeSettings from {filePath}: {ex.Message}", LogLevel.Error);
			}
			return false;
		}

		/// <summary>
		/// Try to save the GameModeSettings to file. Returns true if successful, false otherwise.
		/// </summary>
		public bool SaveToFile(string fileName)
		{
			string filePath = Path.GetFullPath(Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), "Map_Presets", fileName));
			try
			{
				SerializeHelper.SaveAbstractClassToFile(filePath, this);
				Log($"GameModeSettings saved to {filePath}", LogLevel.Information);
				return true;
			}
			catch (Exception ex)
			{
				Log($"Failed to save GameModeSettings to {filePath}: {ex.Message}", LogLevel.Error);
			}
			return false;
		}
	}
}