using Alliance.Common.Core.Utils;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace Alliance.Common.Patch.Utilities
{
	/// <summary>
	/// Used to fix custom banner codes not being applied to tableau armors / banners.
	/// Send and retrieve the banner code from culture + colors instead of sending it whole.
	/// </summary>
	public class BannerToCultureHelper
	{
		private static readonly Dictionary<string, string> bannerCodeToCultureCache = new Dictionary<string, string>();
		private static readonly Dictionary<string, string> cultureToBannerCodeCache = new Dictionary<string, string>();
		private static readonly Dictionary<string, Banner> cultureToBannerCache = new Dictionary<string, Banner>();

		public static string GetCultureFromBannerCode(string bannerCode)
		{
			if (bannerCode.IsEmpty()) return string.Empty;

			if (bannerCodeToCultureCache.TryGetValue(bannerCode, out string cachedCulture))
			{
				return cachedCulture;
			}

			string matchingCulture = "";
			Banner banner = new Banner(bannerCode, 4286777352, 4286777352); // Use default colors for comparison
																			// Find the culture that has this banner
			foreach (KeyValuePair<string, BasicCultureObject> kvpCulture in Factions.Instance.AvailableCultures)
			{
				Banner bannerToCompare = new Banner(kvpCulture.Value.BannerKey, 4286777352, 4286777352);
				if (banner.IsContentsSameWith(bannerToCompare))
				{
					matchingCulture = kvpCulture.Key;
					break;
				}
			}
			// Cache the value for later access
			bannerCodeToCultureCache[bannerCode] = matchingCulture;

			return matchingCulture;
		}

		public static string GetBannerCodeFromCulture(string cultureName, uint primaryColor, uint iconColor)
		{
			if (cultureName.IsEmpty()) return string.Empty;

			string cacheKey = $"{cultureName}-{primaryColor}-{iconColor}";
			if (cultureToBannerCodeCache.TryGetValue(cacheKey, out string cachedBannerCode))
			{
				return cachedBannerCode;
			}

			if (Factions.Instance.AvailableCultures.TryGetValue(cultureName, out BasicCultureObject culture))
			{
				Banner cultureBanner = new Banner(culture.BannerKey);
				// Only change color if the culture has defined color variation
				// Otherwise we assume it wants to preserve color integrity
				if (culture.BackgroundColor1 != culture.BackgroundColor2)
				{
					cultureBanner.ChangePrimaryColor(primaryColor);
					cultureBanner.ChangeIconColors(iconColor);
				}
				cultureToBannerCodeCache[cacheKey] = cultureBanner.Serialize();
				return cultureToBannerCodeCache[cacheKey];
			}

			return string.Empty;
		}

		public static Banner GetBannerFromCulture(string cultureName, uint primaryColor, uint iconColor)
		{
			if (cultureName.IsEmpty()) return Banner.CreateRandomBanner();

			string cacheKey = $"{cultureName}-{primaryColor}-{iconColor}";
			if (cultureToBannerCache.TryGetValue(cacheKey, out Banner cachedBanner))
			{
				return cachedBanner;
			}

			if (Factions.Instance.AvailableCultures.TryGetValue(cultureName, out BasicCultureObject culture))
			{
				Banner cultureBanner = new Banner(culture.BannerKey);
				// Only change color if the culture has defined color variation
				// Otherwise we assume it wants to preserve color integrity
				if (culture.BackgroundColor1 != culture.BackgroundColor2)
				{
					cultureBanner.ChangePrimaryColor(primaryColor);
					cultureBanner.ChangeIconColors(iconColor);
				}
				cultureToBannerCache[cacheKey] = cultureBanner;
				return cultureToBannerCache[cacheKey];
			}

			return Banner.CreateRandomBanner();
		}
	}
}
