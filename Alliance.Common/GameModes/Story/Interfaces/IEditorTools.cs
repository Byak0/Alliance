using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Interfaces
{
	public interface IEditorTools
	{
		public void Tick(float dt);
		public void OpenPlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback);
		/// <summary>Returns the live editor (fly) camera frame, or null if not available.</summary>
		public MatrixFrame? CaptureEditorCameraFrame();
		public bool IsPreviewing { get; }
		public bool IsPreviewPaused { get; }
		public void PlayPreview(Cinematic cinematic, Action<float> onTime, Action onFinished);
		public void PausePreview();
		public void ResumePreview();
		public void SeekPreview(float time);
		public void SamplePreview();
		public void StopPreview();
		public void AddZoneToEditor(Zone zone, string zoneName, Action onEditCallback);
		public void RemoveZoneFromEditor(Zone zone);
		public void SetEditableZone(Zone zone);
		public void ClearZones();
		public void OpenEditor(object obj, Action<object> onCloseCallback);
		public void BeginEntityPick(Action<GameEntityRef> onPicked);
	}
}
