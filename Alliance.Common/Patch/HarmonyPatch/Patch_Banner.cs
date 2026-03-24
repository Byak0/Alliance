using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch Banner to allow more than 32 icons like before 1.3.
	/// </summary>
	class Patch_Banner
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_Banner));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				Harmony.Patch(
					typeof(Banner).GetMethod(nameof(Banner.TryGetBannerDataFromCode), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_Banner).GetMethod(
						nameof(Prefix_TryGetBannerDataFromCode), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_Banner)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Replaces the original TryGetBannerDataFromCode method to remove the limit of 32 icons.
		/// </summary>
		public static bool Prefix_TryGetBannerDataFromCode(string bannerCode, out List<BannerData> bannerDataList, ref bool __result)
		{
			bannerDataList = new List<BannerData>();
			string[] array = bannerCode.Split(new char[] { '.' });
			int num = 0;
			while (num + 10 <= array.Length)
			{
				if (!int.TryParse(array[num], out int num2) || !int.TryParse(array[num + 1], out int num3) || !int.TryParse(array[num + 2], out int num4) || !int.TryParse(array[num + 3], out int num5) || !int.TryParse(array[num + 4], out int num6) || !int.TryParse(array[num + 5], out int num7) || !int.TryParse(array[num + 6], out int num8) || !int.TryParse(array[num + 7], out int num9) || !int.TryParse(array[num + 8], out int num10) || !int.TryParse(array[num + 9], out int num11))
				{
					bannerDataList.Clear();
					__result = false;
					return false;
				}
				BannerData bannerData = new BannerData(num2, num3, num4, new Vec2((float)num5, (float)num6), new Vec2((float)num7, (float)num8), num9 == 1, num10 == 1, (float)num11 * 0.0027777778f);
				bannerDataList.Add(bannerData);
				num += 10;
			}
			// Remove the limit of 32 icons
			//if (bannerDataList.Count > 32)
			//{
			//	bannerDataList.RemoveRange(31, bannerDataList.Count - 32);
			//}

			__result = true;

			return false; // Skip original method
		}
	}
}