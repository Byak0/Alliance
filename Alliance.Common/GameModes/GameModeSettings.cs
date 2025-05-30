﻿using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes
{
	/// <summary>
	/// Store Game Mode informations and list of options.
	/// </summary>
	public class GameModeSettings
	{
		[ScenarioEditor(isEditable: false)]
		public string GameMode;

		[ScenarioEditor(isEditable: false)]
		public string GameModeName;

		[ScenarioEditor(isEditable: false)]
		public string GameModeDescription;

		[ScenarioEditor(label: "Native options", tooltip: "Native options from TW.")]
		public TWConfig TWOptions;

		[ScenarioEditor(label: "Mod options", tooltip: "Additional options from Alliance.")]
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