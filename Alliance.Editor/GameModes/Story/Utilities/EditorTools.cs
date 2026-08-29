using Alliance.Common.Extensions.Cinematics;
using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Views;
using Alliance.Editor.GameModes.Story.ViewModels;
using Alliance.Editor.GameModes.Story.Views;
using Alliance.Editor.Patch.HarmonyPatch;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.GameModes.Story.Utilities
{
	public class EditorTools : IEditorTools
	{
		private ObjectEditorWindow _objectEditorWindow;
		private PlayerSpawnMenuView _playerSpawnMenuView;
		private readonly CinematicView _cinematicView;
		private Action<float> _onPreviewTime;
		private Action _onPreviewFinished;
		private bool _previewPaused;

		public EditorTools()
		{
			_playerSpawnMenuView = new PlayerSpawnMenuView();
			_playerSpawnMenuView.OnBehaviorInitialize();

			_cinematicView = new CinematicView();
			_cinematicView.OnBehaviorInitialize();
		}

		public void Tick(float dt)
		{
			_playerSpawnMenuView.OnMissionTick(dt);
			TickPreview(dt);
			DrawCameraPath();
			EditZoneView.Tick(dt);
			EditEntityView.Tick(dt);
			EditFrameView.Tick(dt);
		}

		public void OpenPlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback)
		{
			_playerSpawnMenuView.OpenMenu(playerSpawnMenu, onCloseCallback, true);
		}

		public MatrixFrame? CaptureEditorCameraFrame()
		{
			if (MBEditor._editorScene == null) return null;
			return MBEditor._editorScene.LastFinalRenderCameraFrame;
		}

		private void DrawCameraPath()
		{
			if (_cinematicView.IsPlaying) return;
			Cinematic cinematic = EditorToolsManager.ActiveEditingCinematic;
			if (cinematic == null) return;
			CameraTrack track = cinematic.FindTrack<CameraTrack>();
			if (track == null || track.Keyframes == null || track.Keyframes.Count == 0) return;

			uint pathColor = new Color(0.3f, 0.7f, 1f).ToUnsignedInteger();
			uint keyColor = new Color(1f, 0.8f, 0.2f).ToUnsignedInteger();
			uint cutColor = new Color(0.5f, 0.5f, 0.5f).ToUnsignedInteger();

			var frames = new System.Collections.Generic.List<MatrixFrame>();
			foreach (var kf in track.Keyframes)
			{
				MatrixFrame f = kf.Frame.ToFrame();
				frames.Add(f);
				Debug.RenderDebugSphere(f.origin, 0.25f, keyColor, true);
				Debug.RenderDebugText3D(f.origin + new Vec3(0, 0, 0.6f), $"{kf.Time:F1}s", keyColor, -100);
				Debug.RenderDebugLine(f.origin, -f.rotation.u * 2f, keyColor, true);
			}

			for (int i = 0; i < frames.Count - 1; i++)
			{
				Interpolation interp = track.Keyframes[i + 1].Interpolation;
				Vec3 p1 = frames[i].origin;
				Vec3 p2 = frames[i + 1].origin;

				if (interp == Interpolation.Constant)
				{
					Vec3 delta = p2 - p1;
					float len = delta.Length;
					Vec3 stubDir = len > 0.001f ? delta / len : Vec3.Zero;
					Debug.RenderDebugLine(p1, stubDir * 0.5f, cutColor, true);
					Debug.RenderDebugLine(p2, -stubDir * 0.5f, cutColor, true);
					continue;
				}

				if (interp == Interpolation.CatmullRom)
				{
					Vec3 p0 = i > 0 ? frames[i - 1].origin : p1;
					Vec3 p3 = i + 2 < frames.Count ? frames[i + 2].origin : p2;
					Vec3 last = p1;
					for (int j = 1; j <= 16; j++)
					{
						float t = (float)j / 16;
						Vec3 pt = KeyframeEvaluator.CatmullRom(p0, p1, p2, p3, t, track.Keyframes[i + 1].Tension);
						Debug.RenderDebugLine(last, pt - last, pathColor, true);
						last = pt;
					}
				}
				else
				{
					Debug.RenderDebugLine(p1, p2 - p1, pathColor, true);
				}
			}
		}

		public bool IsPreviewing => _cinematicView.IsPlaying;
		public bool IsPreviewPaused => _previewPaused;

		public void PlayPreview(Cinematic cinematic, Action<float> onTime, Action onFinished)
		{
			if (cinematic == null) return;

			if (_cinematicView.IsPlaying)
			{
				_onPreviewFinished = null;
				_cinematicView.StopCinematic();
			}

			_onPreviewTime = onTime;
			_onPreviewFinished = onFinished;
			_previewPaused = false;

			_cinematicView.EditorSceneView = MBEditor.GetEditorSceneView();
			Patch_SceneEditorScreen.ActiveCinematicView = _cinematicView;
			_cinematicView.PlayCinematic(cinematic, 0f, false, false);
		}

		public void PausePreview() { _previewPaused = true; }
		public void ResumePreview() { _previewPaused = false; }
		public void SeekPreview(float time) => _cinematicView.Seek(time);
		public void SamplePreview() => _cinematicView.SampleOnce();

		public void StopPreview()
		{
			Action finished = _onPreviewFinished;
			_onPreviewTime = null;
			_onPreviewFinished = null;
			_previewPaused = false;

			_cinematicView.StopCinematic();
			_cinematicView.EditorSceneView = null;
			Patch_SceneEditorScreen.ActiveCinematicView = null;

			finished?.Invoke();
		}

		private void TickPreview(float dt)
		{
			if (!_cinematicView.IsPlaying) return;
			if (_previewPaused) return;
			_cinematicView.OnPreDisplayMissionTick(dt);
			_onPreviewTime?.Invoke(_cinematicView.CurrentTime);
			if (!_cinematicView.IsPlaying) StopPreview();
		}

		public void AddZoneToEditor(Zone zone, string zoneName, Action onEditCallback) => EditZoneView.AddZone(zone, zoneName, onEditCallback);
		public void RemoveZoneFromEditor(Zone zone) => EditZoneView.RemoveZone(zone);
		public void ClearZones() => EditZoneView.ClearZones();
		public void SetEditableZone(Zone zone) => EditZoneView.SetEditableZone(zone);
		public void BeginEntityPick(Action<GameEntityRef> onPicked) => EditEntityView.BeginPick(onPicked);

		public void OpenEditor(object obj, Action<object> onCloseCallback)
		{
			if (_objectEditorWindow == null || !_objectEditorWindow.IsLoaded)
			{
				_objectEditorWindow = new ObjectEditorWindow(obj);
				_objectEditorWindow.Show();
				_objectEditorWindow.Closed += (s, e) =>
				{
					object modified = (_objectEditorWindow.DataContext as ObjectEditorViewModel)?.Object;
					onCloseCallback?.Invoke(modified);
					_objectEditorWindow = null;
				};
			}
			else
			{
				_objectEditorWindow.Focus();
			}
		}
	}
}
