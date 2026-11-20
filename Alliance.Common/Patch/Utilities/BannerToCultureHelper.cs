using Alliance.Common.Core.Utils;
using System;
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
				Banner bannerToCompare = new Banner(kvpCulture.Value.Banner.BannerCode, 4286777352, 4286777352);
				if (IsBannerSame(banner, bannerToCompare))
				{
					matchingCulture = kvpCulture.Key;
					break;
				}
			}
			// Cache the value for later access
			bannerCodeToCultureCache[bannerCode] = matchingCulture;

			return matchingCulture;
		}

		public static bool IsBannerSame(Banner banner, Banner otherBanner)
		{
			if (banner == null && otherBanner == null)
			{
				return true;
			}

			if (banner == null || otherBanner == null)
			{
				return false;
			}

			if (banner.GetBannerDataListCount() != otherBanner.GetBannerDataListCount())
			{
				return false;
			}

			for (int i = 0; i < banner.GetBannerDataListCount(); i++)
			{
				BannerData bannerDataAtIndex = banner.GetBannerDataAtIndex(i);
				BannerData bannerDataAtIndex2 = otherBanner.GetBannerDataAtIndex(i);
				if (!IsBannerDataSame(bannerDataAtIndex,bannerDataAtIndex2))
				{
					return false;
				}
			}

			return true;
		}

		public static bool IsBannerDataSame(BannerData data1, BannerData data2)
		{
			if (data1 is BannerData bannerData1 && data2 is BannerData bannerData2 
				&& bannerData1.MeshId == bannerData2.MeshId 
				&& bannerData1.ColorId == bannerData2.ColorId 
				&& bannerData1.ColorId2 == bannerData2.ColorId2
				&& bannerData1.Size.X == bannerData2.Size.X 
				&& bannerData1.Size.Y == bannerData2.Size.Y 
				&& bannerData1.Position.X == bannerData2.Position.X 
				&& bannerData1.Position.Y == bannerData2.Position.Y 
				&& bannerData1.DrawStroke == bannerData2.DrawStroke 
				&& bannerData1.Mirror == bannerData2.Mirror 
				&& Math.Round(bannerData1.RotationValue, 2) == Math.Round(bannerData2.RotationValue, 2)) // Round rotation value to 2 decimals to avoid minor precision differences
			{
				return true;
			}

			return false;
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
				Banner cultureBanner = new Banner(culture.Banner.BannerCode);
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
				Banner cultureBanner = new Banner(culture.Banner.BannerCode);
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
