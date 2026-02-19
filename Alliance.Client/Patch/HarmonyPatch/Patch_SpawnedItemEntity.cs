using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
	class Patch_SpawnedItemEntity
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_SpawnedItemEntity));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;
				Harmony.Patch(
					typeof(SpawnedItemEntity).GetMethod(nameof(SpawnedItemEntity.StopPhysicsAndSetFrameForClient),
						BindingFlags.Instance | BindingFlags.Public),
					postfix: new HarmonyMethod(typeof(Patch_SpawnedItemEntity).GetMethod(
						nameof(Postfix_StopPhysicsAndSetFrameForClient), BindingFlags.Static | BindingFlags.Public)));

			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_SpawnedItemEntity)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		// Fix banners not being focusable after being dropped
		public static void Postfix_StopPhysicsAndSetFrameForClient(SpawnedItemEntity __instance)
		{
			if (!__instance.IsBanner())
				return;

			if (__instance.GameEntity == null)
				return;

			using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
			{
				// Convert to a raycast body so it stays fixed but can be hit by focus raycasts.
				__instance.GameEntity.SetPhysicsMoveToBatched(true);
				__instance.GameEntity.ConvertDynamicBodyToRayCast();
			}
		}
	}
}
