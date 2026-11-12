using Alliance.Common.Extensions.Cinematics;
using HarmonyLib;
using System;
using System.Reflection;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.Patch.HarmonyPatch
{
	class Patch_SceneEditorScreen
	{
		private static readonly Harmony Harmony = new(SubModule.ModuleId + nameof(Patch_SceneEditorScreen));
		private static bool _patched;

		public static CinematicView ActiveCinematicView;

		public static bool Patch()
		{
			if (_patched) return false;
			try
			{
				Type screenType = AccessTools.TypeByName("TaleWorlds.MountAndBlade.View.Screens.SceneEditorScreen");
				if (screenType == null)
				{
					Log($"{nameof(Patch_SceneEditorScreen)}: SceneEditorScreen type not found", LogLevel.Error);
					return false;
				}
				MethodInfo original = AccessTools.Method(screenType, "OnFrameTick");
				Harmony.Patch(original, postfix: new HarmonyMethod(typeof(Patch_SceneEditorScreen), nameof(Postfix)));
				_patched = true;
				return true;
			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_SceneEditorScreen)}: {e}", LogLevel.Error);
				return false;
			}
		}

		// SceneEditorScreen.OnFrameTick ends by calling MBEditor.TickSceneEditorPresentation,
		// which updates the native fly camera. This postfix reapplies the cinematic camera
		// so preview playback overrides the fly camera without disabling the editor's scene view
		// (disabling it would break GauntletUI overlay rendering).
		static void Postfix()
		{
			if (ActiveCinematicView?.IsPlaying == true)
				ActiveCinematicView.ReapplyEditorCamera();
		}
	}
}
