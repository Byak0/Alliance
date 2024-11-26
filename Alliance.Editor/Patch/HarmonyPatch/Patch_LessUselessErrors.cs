using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch some classes to remove completely useless and annoying asserts.
	/// </summary>
	class Patch_LessUselessErrors
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_LessUselessErrors));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				// Patch SetScale method to remove scale check
				Harmony.Patch(
					typeof(WeaponDesignElement).GetMethod(nameof(WeaponDesignElement.SetScale), BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_LessUselessErrors).GetMethod(
						nameof(Prefix_WeaponDesignElement_SetScale), BindingFlags.Static | BindingFlags.Public)));

				// Patch GetBannerDataFromBannerCode method to remove icon count check
				Harmony.Patch(
					typeof(Banner).GetMethod(nameof(Banner.GetBannerDataFromBannerCode), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_LessUselessErrors).GetMethod(
						nameof(Prefix_Banner_GetBannerDataFromBannerCode), BindingFlags.Static | BindingFlags.Public)));

				// Patch GenerateActionSetNameWithSuffix method for more logical action sets
				Harmony.Patch(
					typeof(ActionSetCode).GetMethod(nameof(ActionSetCode.GenerateActionSetNameWithSuffix), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_LessUselessErrors).GetMethod(
						nameof(Prefix_ActionSetCode_GenerateActionSetNameWithSuffix), BindingFlags.Static | BindingFlags.Public)));

				// Patch BannerVisual.GetMeshMatrix method to remove useless rotation check
				Harmony.Patch(
					typeof(BannerVisual).GetMethod(nameof(BannerVisual.GetMeshMatrix), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_LessUselessErrors).GetMethod(
						nameof(Prefix_BannerVisual_GetMeshMatrix), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_LessUselessErrors)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Replace WeaponDesignElement.SetScale method to remove scale check
		/// </summary>
		public static bool Prefix_WeaponDesignElement_SetScale(ref int scalePercentage, WeaponDesignElement __instance, ref int ____scalePercentage)
		{
			// Removed scale check, useless and annoying. We have used scales out of these limits for years.
			//Debug.Assert(scalePercentage >= 90 && scalePercentage <= 110, "ScaleFactor must be a value between 90% and 110%", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.Core\\WeaponDesignElement.cs", "SetScale", 96);			
			//Debug.Assert(__instance.IsValid, "A scaling piece must be valid.", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.Core\\WeaponDesignElement.cs", "SetScale", 97);

			____scalePercentage = scalePercentage;

			return false; // Skip original method
		}

		/// <summary>
		/// Replace Banner.GetBannerDataFromBannerCode method to remove icon count check
		/// </summary>
		public static bool Prefix_Banner_GetBannerDataFromBannerCode(string bannerCode, ref List<BannerData> __result)
		{
			__result = new List<BannerData>();
			string[] array = bannerCode.Split(new char[] { '.' });
			int num = 0;
			while (num + 10 <= array.Length)
			{
				BannerData bannerData = new BannerData(int.Parse(array[num]), int.Parse(array[num + 1]), int.Parse(array[num + 2]), new Vec2((float)int.Parse(array[num + 3]), (float)int.Parse(array[num + 4])), new Vec2((float)int.Parse(array[num + 5]), (float)int.Parse(array[num + 6])), int.Parse(array[num + 7]) == 1, int.Parse(array[num + 8]) == 1, (float)int.Parse(array[num + 9]) * 0.00278f);
				__result.Add(bannerData);
				num += 10;
			}

			// Remove icon limit check
			//Debug.Assert(list.Count < 33, "[DEBUG]maximum icon count is exceeded!", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.Core\\Banner.cs", "GetBannerDataFromBannerCode", 555);			

			return false; // Skip original method
		}

		/// <summary>
		/// Return more logical action set. IE don't return as_uruk_villager if uruk use as_human_warrior as base...
		/// </summary>
		public static bool Prefix_ActionSetCode_GenerateActionSetNameWithSuffix(Monster monster, bool isFemale, string suffix, ref string __result)
		{
			if (monster == null || monster.ActionSetCode == "as_human_warrior")
			{
				__result = "as_human" + (isFemale ? "_female" : "") + suffix;
			}
			else
			{
				__result = "as_" + (string.IsNullOrEmpty(monster.BaseMonster) ? monster.StringId : monster.BaseMonster) + (isFemale ? "_female" : "") + suffix;
			}

			return false; // Skip original method
		}

		/// <summary>
		/// Remove rotation check when generating mesh from Banner code. Because we need negative rotation and it works.
		/// </summary>
		public static bool Prefix_BannerVisual_GetMeshMatrix(ref Mesh mesh, float marginLeft, float marginTop, float width, float height, bool mirrored, float rotation, float deltaZ, ref MatrixFrame __result)
		{
			__result = MatrixFrame.Identity;
			float num = width / 1528f;
			float num2 = height / 1528f;
			float num3 = num / mesh.GetBoundingBoxWidth();
			float num4 = num2 / mesh.GetBoundingBoxHeight();
			__result.rotation.RotateAboutUp(rotation);
			if (mirrored)
			{
				__result.rotation.RotateAboutForward(3.1415927f);
			}
			__result.rotation.ApplyScaleLocal(new Vec3(num3, num4, 1f, -1f));
			__result.origin.x = 0f;
			__result.origin.y = 0f;
			__result.origin.x = __result.origin.x + marginLeft / 1528f;
			__result.origin.y = __result.origin.y - marginTop / 1528f;
			__result.origin.z = __result.origin.z + deltaZ;

			// Remove this useless check on rotation which trigger on negative values that we need
			//Debug.Assert(rotation >= 0f && rotation <= 6.2831855f, "rotation >= 0 && rotation <= 2 * MBMath.PI", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\BannerVisual.cs", "GetMeshMatrix", 78);

			return false; // Skip original method
		}
	}
}