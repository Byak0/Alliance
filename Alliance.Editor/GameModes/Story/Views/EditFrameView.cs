using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.GameModes.Story.Views
{
	/// <summary>
	/// Display ghost previews of GameEntity for actions who need them.
	/// </summary>
	public static class EditFrameView
	{
		private class GhostEntry
		{
			public object Owner;
			public Func<string> LabelGetter;
			public Func<string> KeyGetter;
			public Func<GameEntity> GhostFactory;
			public Func<MatrixFrame> FrameGetter;
			public uint Color;
			public string CurrentKey = "\0";   // sentinel so the first Ensure builds
			public GameEntity Ghost;
		}

		private static readonly List<GhostEntry> _entries = new();
		private static readonly uint[] _palette =
		{
			new Color(0f, 1f, 1f).ToUnsignedInteger(),   // cyan
			new Color(1f, 0.6f, 0f).ToUnsignedInteger(), // orange
			new Color(0.4f, 1f, 0.4f).ToUnsignedInteger(),// green
			new Color(0.9f, 0.4f, 1f).ToUnsignedInteger(),// magenta
			new Color(1f, 1f, 0.3f).ToUnsignedInteger()  // yellow
		};
		private static SceneView _sceneView;

		private static GhostEntry _placing;
		private static Action<FrameValue> _onPlaced;
		private static float _yaw;
		private static Vec3 _baseEuler;
		private static Vec3 _baseScale;
		private static bool _hasGround;
		private static bool _leftWasDown = true;
		private static bool _rightClickHeld;
		private static float _rightClickHoldTime;

		public static bool IsPlacing => _placing != null;

		public static void ShowGhost(object owner, Func<string> labelGetter, Func<string> keyGetter,
			Func<GameEntity> ghostFactory, Func<MatrixFrame> frameGetter)
		{
			Remove(owner);
			var entry = new GhostEntry
			{
				Owner = owner,
				LabelGetter = labelGetter,
				KeyGetter = keyGetter,
				GhostFactory = ghostFactory,
				FrameGetter = frameGetter,
				Color = _palette[_entries.Count % _palette.Length]
			};
			_entries.Add(entry);
			_sceneView = MBEditor.GetEditorSceneView();
		}

		public static void HideGhost(object owner)
		{
			if (_placing?.Owner == owner) CancelPlacement();
			Remove(owner);
		}

		public static void BeginPlace(object owner, Action<FrameValue> onPlaced, MatrixFrame baseFrame)
		{
			GhostEntry entry = _entries.Find(e => ReferenceEquals(e.Owner, owner));
			if (entry == null) return;
			_placing = entry;
			_onPlaced = onPlaced;
			_leftWasDown = true;
			_rightClickHeld = false;
			_rightClickHoldTime = 0f;
			_baseEuler = baseFrame.rotation.GetEulerAngles();
			_baseScale = baseFrame.rotation.GetScaleVector();
			_yaw = _baseEuler.z;
			Log($"[EditFrameView] Place mode for '{entry.LabelGetter?.Invoke()}' (wheel = rotate, click = confirm, right-click = cancel).", LogLevel.Debug);
		}

		public static void Tick(float dt)
		{
			// Rebuild any ghost whose source changed.
			foreach (var e in _entries) EnsureEntryGhost(e);

			if (_placing != null)
			{
				TickPlacement(dt);
				// Non-placing ghosts still follow their frames.
				foreach (var e in _entries) if (e != _placing) PositionAndLabel(e);
			}
			else
			{
				foreach (var e in _entries) PositionAndLabel(e);
			}
		}

		private static void PositionAndLabel(GhostEntry e)
		{
			MatrixFrame frame = e.FrameGetter();
			if (e.Ghost != null) e.Ghost.SetGlobalFrame(frame);
			RenderLabel(e, frame);
		}

		private static void TickPlacement(float dt)
		{
			HandleCancel(dt);
			if (_placing == null) return;

			if (_sceneView != null)
			{
				if (Input.IsKeyDown(InputKey.MouseScrollDown)) _yaw -= 0.1f;
				else if (Input.IsKeyDown(InputKey.MouseScrollUp)) _yaw += 0.1f;
			}

			MatrixFrame frame = PlacementFrame();
			if (_placing.Ghost != null) _placing.Ghost.SetGlobalFrame(frame);
			else if (_hasGround) Debug.RenderDebugSphere(frame.origin, 0.5f, _placing.Color, true);
			RenderLabel(_placing, frame);

			bool leftDown = Input.IsKeyDown(InputKey.LeftMouseButton);
			if (leftDown && !_leftWasDown && _hasGround)
			{
				FrameValue captured = FrameValue.FromFrame(frame);
				var cb = _onPlaced;
				CancelPlacement();
				cb?.Invoke(captured);
			}
			_leftWasDown = leftDown;
		}

		private static MatrixFrame PlacementFrame()
		{
			MatrixFrame frame = MatrixFrame.Identity;
			_hasGround = false;
			if (_sceneView != null)
			{
				_sceneView.ProjectedMousePositionOnGround(out Vec3 ground, out _, true, BodyFlags.BodyOwnerFlora, checkOccludedSurface: true);
				frame.origin = ground;
				_hasGround = true;
			}
			frame.rotation.ApplyEulerAngles(new Vec3(_baseEuler.x, _baseEuler.y, _yaw));
			if (_baseScale.x != 1f || _baseScale.y != 1f || _baseScale.z != 1f)
			{
				frame.rotation.ApplyScaleLocal(_baseScale);
			}
			Log($"{frame.origin}");
			return frame;
		}

		private static void HandleCancel(float dt)
		{
			if (Input.IsKeyDown(InputKey.RightMouseButton))
			{
				_rightClickHoldTime += dt;
				if (_rightClickHoldTime > 0.2f) _rightClickHeld = true;
			}
			else if (Input.IsKeyReleased(InputKey.RightMouseButton))
			{
				if (!_rightClickHeld)
				{
					Log("[EditFrameView] Place cancelled.", LogLevel.Debug);
					CancelPlacement();
				}
				_rightClickHeld = false;
				_rightClickHoldTime = 0f;
			}
		}

		private static void CancelPlacement()
		{
			_placing = null;
			_onPlaced = null;
		}

		private static void EnsureEntryGhost(GhostEntry e)
		{
			string key = e.KeyGetter?.Invoke() ?? "";
			if (key == e.CurrentKey) return;

			DestroyGhost(e);
			e.CurrentKey = key;
			if (string.IsNullOrEmpty(key)) return;

			e.Ghost = e.GhostFactory?.Invoke();
			if (e.Ghost != null)
			{
				e.Ghost.SetMobility(GameEntity.Mobility.Dynamic);
				e.Ghost.SetVisibilityExcludeParents(true);
				e.Ghost.AddBodyFlags(BodyFlags.DoNotCollideWithRaycast | BodyFlags.CommonFlagsThatDoNotBlockRay);
			}
			else Log($"[EditFrameView] Ghost factory returned null for '{e.LabelGetter?.Invoke()}'.", LogLevel.Warning);
		}

		private static void RenderLabel(GhostEntry e, MatrixFrame frame)
		{
			string label = e.LabelGetter?.Invoke();
			if (string.IsNullOrEmpty(label)) return;
			Debug.RenderDebugText3D(frame.origin + new Vec3(0f, 0f, 1.5f), label, e.Color, -100);
		}

		private static void Remove(object owner)
		{
			int i = _entries.FindIndex(e => ReferenceEquals(e.Owner, owner));
			if (i < 0) return;
			DestroyGhost(_entries[i]);
			_entries.RemoveAt(i);
		}

		private static void DestroyGhost(GhostEntry e)
		{
			if (e.Ghost != null)
			{
				e.Ghost.Remove(0);
				e.Ghost = null;
			}
		}
	}
}
