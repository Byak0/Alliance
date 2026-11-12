using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
	/// <summary>
	/// PreloadHelper.WaitForMeshesToBeLoaded loops forever when a preloaded item references a
	/// physics shape that is not a valid resource (PhysicsShape.GetFromResource returns null on
	/// every pass), freezing the client on the loading screen. Replacement: bounded wait, then log
	/// the unresolved resources and move on. Patched because PreloadHelper is instantiated inside
	/// native views - no clean override point.
	/// </summary>
	class Patch_PreloadHelper
	{
		private const int TimeoutMs = 30000;

		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_PreloadHelper));

		private static FieldInfo _metaMeshesField;
		private static FieldInfo _metaMeshItemField;
		private static FieldInfo _physicsShapesField;

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;

				_metaMeshesField = AccessTools.Field(typeof(PreloadHelper), "_uniqueMetaMeshes");
				_physicsShapesField = AccessTools.Field(typeof(PreloadHelper), "_uniqueDynamicPhysicsShapeName");
				_metaMeshItemField = _metaMeshesField?.FieldType.GetGenericArguments()[0].GetField("Item1");

				Harmony.Patch(
					typeof(PreloadHelper).GetMethod(nameof(PreloadHelper.WaitForMeshesToBeLoaded),
						BindingFlags.Instance | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_PreloadHelper).GetMethod(
						nameof(Prefix_WaitForMeshesToBeLoaded), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_PreloadHelper)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		public static bool Prefix_WaitForMeshesToBeLoaded(PreloadHelper __instance)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedMilliseconds < TimeoutMs)
			{
				if (CountPendingResources(__instance, out _, out _) == 0) return false;
				Thread.Sleep(1);
			}

			CountPendingResources(__instance, out List<string> pendingMeshes, out List<string> missingShapes);
			foreach (string shape in missingShapes)
				Log($"Alliance - Preload timeout: physics shape '{shape}' is not a valid resource. Fix the body_name/holster_body_name/collision_body of the item using it.", LogLevel.Error);
			foreach (string mesh in pendingMeshes)
				Log($"Alliance - Preload timeout: mesh '{mesh}' did not finish loading.", LogLevel.Error);
			Log($"Alliance - Preload gave up after {TimeoutMs / 1000}s ({missingShapes.Count} missing physics shapes, {pendingMeshes.Count} pending meshes), continuing anyway.", LogLevel.Warning);
			return false;
		}

		private static int CountPendingResources(PreloadHelper instance, out List<string> pendingMeshes, out List<string> missingShapes)
		{
			int pending = 0;
			pendingMeshes = new List<string>();
			missingShapes = new List<string>();

			if (_metaMeshesField?.GetValue(instance) is IEnumerable meshes && _metaMeshItemField != null)
			{
				foreach (object meshTuple in meshes)
				{
					if (_metaMeshItemField.GetValue(meshTuple) is MetaMesh mesh && mesh.CheckResources() > 0)
					{
						pending++;
						pendingMeshes.Add(mesh.GetName());
					}
				}
			}

			if (_physicsShapesField?.GetValue(instance) is HashSet<string> shapes)
			{
				foreach (string shape in shapes)
				{
					if (PhysicsShape.GetFromResource(shape, mayReturnNull: true) == null)
					{
						pending++;
						missingShapes.Add(shape);
					}
				}
			}
			return pending;
		}
	}
}
