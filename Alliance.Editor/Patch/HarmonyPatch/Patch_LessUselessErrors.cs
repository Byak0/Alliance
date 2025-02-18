using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
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

				// Patch BannerVisual.GetMeshMatrix method to remove useless rotation check
				Harmony.Patch(
					typeof(BannerVisual).GetMethod(nameof(BannerVisual.GetMeshMatrix), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_LessUselessErrors).GetMethod(
						nameof(Prefix_BannerVisual_GetMeshMatrix), BindingFlags.Static | BindingFlags.Public)));

				// Patch MissionWeapon constructor
				Harmony.Patch(
					typeof(MissionWeapon).GetConstructor(new[] { typeof(ItemObject), typeof(ItemModifier), typeof(Banner) }),
					prefix: new HarmonyMethod(typeof(Patch_LessUselessErrors).GetMethod(
						nameof(Prefix_MissionWeaponConstructor), BindingFlags.Static | BindingFlags.Public)));
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

		/// <summary>
		/// Replace MissionWeapon constructor for easier debug.
		/// /!\ Cause issues with ItemModifier, need to be fixed.
		/// </summary>
		public static bool Prefix_MissionWeaponConstructor(ItemObject item, ItemModifier itemModifier, Banner banner, ref MissionWeapon __instance, ref List<WeaponComponentData> ____weapons, ref short ____modifiedMaxDataValue, ref bool ____hasAnyConsumableUsage, ref short ____dataValue, ref object ____ammoWeapon, ref object ____attachedWeapons, ref object ____attachedWeaponFrames)
		{
			PropertyInfo propertyInfoItem = typeof(MissionWeapon).GetProperty(nameof(MissionWeapon.Item));
			PropertyInfo propertyInfoItemModifier = typeof(MissionWeapon).GetProperty(nameof(MissionWeapon.ItemModifier));
			PropertyInfo propertyInfoBanner = typeof(MissionWeapon).GetProperty(nameof(MissionWeapon.Banner));
			PropertyInfo propertyInfoGlossMultiplier = typeof(MissionWeapon).GetProperty(nameof(MissionWeapon.GlossMultiplier));
			object boxed = __instance;
			propertyInfoItem.SetValue(boxed, item, null);
			propertyInfoItemModifier.SetValue(boxed, itemModifier, null);
			propertyInfoBanner.SetValue(boxed, banner, null);
			propertyInfoGlossMultiplier.SetValue(boxed, 1f, null);
			__instance = (MissionWeapon)boxed;

			Debug.Assert(__instance.ItemModifier == null || !GameNetwork.IsServerOrRecorder, "ItemModifier == null || !GameNetwork.IsServerOrRecorder", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\MissionWeapon.cs", ".ctor", 201);
			__instance.CurrentUsageIndex = 0;
			____weapons = new List<WeaponComponentData>(1);
			____modifiedMaxDataValue = 0;
			____hasAnyConsumableUsage = false;

			if (item != null)
			{
				Debug.Assert(item.WeaponComponent != null, "item.WeaponComponent != null", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\MissionWeapon.cs", ".ctor", 209);
				if (item.WeaponComponent != null && item.Weapons != null)
				{
					foreach (WeaponComponentData weaponComponentData in item.Weapons)
					{
						____weapons.Add(weaponComponentData);
						bool isConsumable = weaponComponentData.IsConsumable;
						if (isConsumable || weaponComponentData.IsRangedWeapon || weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.HasHitPoints))
						{
							//Debug.Assert(____modifiedMaxDataValue == 0, $"_modifiedMaxDataValue == 0 for {weaponComponentData.WeaponDescriptionId} - {item.Name}", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\MissionWeapon.cs", ".ctor", 218);
							//Debug.Assert(item.PrimaryWeapon == weaponComponentData || (!item.PrimaryWeapon.IsConsumable && !item.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.HasHitPoints)), $"item.PrimaryWeapon == weapon|| (!item.PrimaryWeapon.IsConsumable && !item.PrimaryWeapon.WeaponFlags.HasAnyFlag(WeaponFlags.HasHitPoints)) for {weaponComponentData.WeaponDescriptionId} - {item.Name}", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\MissionWeapon.cs", ".ctor", 219);
							____modifiedMaxDataValue = weaponComponentData.MaxDataValue;
							if (itemModifier != null)
							{
								if (weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.HasHitPoints))
								{
									____modifiedMaxDataValue = weaponComponentData.GetModifiedMaximumHitPoints(itemModifier);
								}
								else if (isConsumable)
								{
									____modifiedMaxDataValue = weaponComponentData.GetModifiedStackCount(itemModifier);
								}
							}
						}
						if (isConsumable)
						{
							____hasAnyConsumableUsage = true;
						}
					}
				}
			}

			____dataValue = ____modifiedMaxDataValue;
			__instance.ReloadPhase = 0;
			____ammoWeapon = null;
			____attachedWeapons = null;
			____attachedWeaponFrames = null;

			return false; // Skip the original constructor
		}
	}
}