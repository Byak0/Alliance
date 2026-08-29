using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models;
using System;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Provides static, solution-wide access to modding kit tools, enabling editing of zones, player spawn menus, and simple objects.
	/// Primarily used with the Scenario Editor and AL_TriggerAction script.
	/// Necessary because the Common project cannot reference the Editor project.
	/// </summary>
	public static class EditorToolsManager
	{
		public static IEditorTools EditorTools;
		public static Cinematic ActiveEditingCinematic;

		public static void OpenPlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback)
		{
			EditorTools?.OpenPlayerSpawnMenu(playerSpawnMenu, onCloseCallback);
		}

		/// <summary>Returns the live editor (fly) camera frame, or null if the editor camera isn't available.
		/// Used by "Capture from camera" on camera keyframes.</summary>
		public static MatrixFrame? CaptureEditorCameraFrame()
		{
			return EditorTools?.CaptureEditorCameraFrame();
		}

		public static bool IsPreviewing => EditorTools?.IsPreviewing ?? false;
		public static bool IsPreviewPaused => EditorTools?.IsPreviewPaused ?? false;
		public static void PlayPreview(Cinematic cinematic, Action<float> onTime, Action onFinished) => EditorTools?.PlayPreview(cinematic, onTime, onFinished);
		public static void PausePreview() => EditorTools?.PausePreview();
		public static void ResumePreview() => EditorTools?.ResumePreview();
		public static void SeekPreview(float time) => EditorTools?.SeekPreview(time);
		public static void SamplePreview() => EditorTools?.SamplePreview();
		public static void StopPreview() => EditorTools?.StopPreview();

		public static void AddZoneToEditor(Zone zone, string zoneName, Action onEditCallback)
		{
			EditorTools?.AddZoneToEditor(zone, zoneName, onEditCallback);
		}

		public static void RemoveZoneFromEditor(Zone zone)
		{
			EditorTools?.RemoveZoneFromEditor(zone);
		}

		public static void SetEditableZone(Zone zone)
		{
			EditorTools?.SetEditableZone(zone);
		}

		public static void ClearZones()
		{
			EditorTools?.ClearZones();
		}

		public static void OpenEditor(object obj, Action<object> onCloseCallback)
		{
			EditorTools?.OpenEditor(obj, onCloseCallback);
		}

		public static void BeginEntityPick(Action<GameEntityRef> onPicked)
		{
			EditorTools?.BeginEntityPick(onPicked);
		}
	}
}
