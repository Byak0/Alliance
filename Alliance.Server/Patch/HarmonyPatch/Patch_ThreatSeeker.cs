using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.RangedSiegeWeaponAi;

namespace Alliance.Server.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch crash when IA controls a siege weapon and has no siege units to target.
	/// </summary>
	class Patch_ThreatSeeker
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_ThreatSeeker));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;
				Harmony.Patch(
					typeof(ThreatSeeker).GetMethod("GetPositionMultiplierOfFormation", BindingFlags.Static | BindingFlags.NonPublic),
					prefix: new HarmonyMethod(typeof(Patch_ThreatSeeker).GetMethod(
						nameof(Prefix_GetPositionMultiplierOfFormation), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_ThreatSeeker)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Fix crash when the referencePositions is empty (probably no siege units to target).
		/// </summary>
		public static bool Prefix_GetPositionMultiplierOfFormation(Formation formation, IEnumerable<ICastleKeyPosition> referencePositions, ref float __result)
		{
			// Check if referencePositions is empty
			if (!referencePositions.Any())
			{
				// Skip the original method and return a default value
				__result = 1f;
				return false;
			}
			else
			{
				// Call the original method
				return true;
			}
		}


		/* Original method 
         * 
		private static float GetPositionMultiplierOfFormation(Formation formation, IEnumerable<ICastleKeyPosition> referencePositions)
		{
			ICastleKeyPosition closestCastlePosition;
			float minimumDistanceBetweenPositions = GetMinimumDistanceBetweenPositions(formation.GetMedianAgent(excludeDetachedUnits: false, excludePlayer: false, formation.GetAveragePositionOfUnits(excludeDetachedUnits: false, excludePlayer: false)).Position, referencePositions, out closestCastlePosition);
			bool flag = closestCastlePosition.AttackerSiegeWeapon != null && closestCastlePosition.AttackerSiegeWeapon.HasCompletedAction();
			float num;
			if (formation.PhysicalClass.IsRanged())
			{
				num = ((minimumDistanceBetweenPositions < 20f) ? 1f : ((!(minimumDistanceBetweenPositions < 35f)) ? 0.6f : 0.8f));
				return num + (flag ? 0.2f : 0f);
			}
			num = ((minimumDistanceBetweenPositions < 15f) ? 0.2f : ((!(minimumDistanceBetweenPositions < 40f)) ? 0.12f : 0.15f));
			return num * (flag ? 7.5f : 1f);
		}*/
	}
}
