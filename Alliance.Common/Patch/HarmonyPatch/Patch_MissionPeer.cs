using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	class Patch_MissionPeer
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionPeer));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;

				var getter = typeof(MissionPeer).GetProperty(nameof(MissionPeer.SelectedPerks), BindingFlags.Instance | BindingFlags.Public).GetGetMethod();
				var prefix = typeof(Patch_MissionPeer).GetMethod(nameof(Prefix_SelectedPerks), BindingFlags.Static | BindingFlags.Public);

				Harmony.Patch(getter, new HarmonyMethod(prefix));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_MissionPeer)}", LogLevel.Error);
				Log(e.Message, LogLevel.Error);
				return false;
			}

			return true;
		}

		// Fix perks not being refreshed when more than 2 are defined
		public static bool Prefix_SelectedPerks(ref MBReadOnlyList<MPPerkObject> __result, MissionPeer __instance, (int, MBList<MPPerkObject>) ____selectedPerks)
		{
			if (__instance.SelectedTroopIndex < 0 || __instance.Team == null || __instance.Team.Side == BattleSideEnum.None)
			{
				// Return an empty list if no valid troop or side
				__result = new MBList<MPPerkObject>();
				return false;
			}

			// Original getter doesn't refresh the selected perks if there are more than 2, we removed that condition
			if (!__instance.RefreshSelectedPerks())
			{
				// Return an empty list if refresh failed
				__result = new MBReadOnlyList<MPPerkObject>();
				return false;
			}

			// Refresh ____selectedPerks, in case it was changed by RefreshSelectedPerks
			____selectedPerks = ((int, MBList<MPPerkObject>))AccessTools.Field(typeof(MissionPeer), "_selectedPerks").GetValue(__instance);

			// Set the result to the refreshed perks list
			__result = ____selectedPerks.Item2;
			return false; // Skip original getter
		}

		// Original method
		/*public MBReadOnlyList<MPPerkObject> SelectedPerks
		{
			get
			{
				if (SelectedTroopIndex < 0 || Team == null || Team.Side == BattleSideEnum.None)
				{
					return new MBList<MPPerkObject>();
				}

				if ((_selectedPerks.Item2 == null || SelectedTroopIndex != _selectedPerks.Item1 || _selectedPerks.Item2.Count < 3) && !RefreshSelectedPerks())
				{
					return new MBReadOnlyList<MPPerkObject>();
				}

				return _selectedPerks.Item2;
			}
		}*/
	}
}
