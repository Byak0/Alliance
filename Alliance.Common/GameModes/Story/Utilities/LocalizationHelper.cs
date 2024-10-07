using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Localization;

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
	}
}
