using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Localization;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Utilities
{
	public static class LocalizationHelper
	{
		/// <summary>
		/// Retrieves the active text language from the game.
		/// </summary>
		public static string GetCurrentLanguage()
		{
			return MBTextManager.ActiveTextLanguage;
		}

		/// <summary>
		/// Get the list of available languages from the game.
		/// </summary>
		public static List<string> GetAvailableLanguages()
		{
			return LocalizedTextManager.GetLanguageIds(developmentMode: false).ToList();
		}

		/// <summary>
		/// Get the index of the specified language.
		/// </summary>
		public static int GetLanguageIndex(string language)
		{
			var languages = GetAvailableLanguages();
			return languages.IndexOf(language);
		}

		/// <summary>
		/// Get the language at the specified index.
		/// </summary>
		public static string GetLanguage(int index)
		{
			var languages = GetAvailableLanguages();
			if (index >= 0 && index < languages.Count)
			{
				return languages[index];
			}
			Log("Invalid language index: " + index, LogLevel.Warning);
			return GetCurrentLanguage();
		}
	}
}
