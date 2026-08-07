using Alliance.Common.Extensions.BuildSystem;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using System.Windows.Controls.Primitives;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.GameModes.Story.Views
{
	/// <summary>
	/// In-scene pick mode for <see cref="GameEntityRef"/> literals. Activated by the
	/// "Select entity on map" button (<see cref="ViewModels.EntityRefViewModel"/>). While active, the next
	/// entity the user selects in the editor viewport is captured: an <see cref="AL_EntityMarker"/> script is
	/// attached (if missing) carrying a fresh GUID, and a <see cref="GameEntityRef"/> is handed back.
	/// Right-click (short tap) cancels pick mode.
	/// </summary>
	public static class EditEntityView
	{
		private const string MarkerScriptName = nameof(AL_EntityMarker);

		private static bool _picking;
		private static Action<GameEntityRef> _onPicked;

		private static bool _rightClickHeld;
		private static float _rightClickHoldTime;

		public static bool IsPicking => _picking;

		public static void BeginPick(Action<GameEntityRef> onPicked)
		{
			_onPicked = onPicked;
			_picking = true;
			_rightClickHeld = false;
			_rightClickHoldTime = 0f;
			Log("[EditEntityView] Pick mode active — select an entity in the scene (right-click to cancel).", LogLevel.Debug);
		}

		public static void CancelPick()
		{
			_picking = false;
			_onPicked = null;
		}

		public static void Tick(float dt)
		{
			if (!_picking) return;

			HandleCancel(dt);

			if (!_picking) return;

			GameEntity selected = GetCurrentlySelectedEntity();
			if (selected != null)
			{
				Log($"[EditEntityView] {selected.Name} selected", LogLevel.Debug);
				GameEntityRef gameRef = AttachMarker(selected);
				_onPicked?.Invoke(gameRef);
				CancelPick();
			}
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
					Log("[EditEntityView] Pick cancelled.", LogLevel.Debug);
					CancelPick();
				}
				_rightClickHeld = false;
				_rightClickHoldTime = 0f;
			}
		}

		private static GameEntity GetCurrentlySelectedEntity()
		{
			List<GameEntity> entities = new List<GameEntity>();
			MBEditor._editorScene?.GetEntities(ref entities);
			foreach (GameEntity entity in entities)
			{
				if (entity.IsSelectedOnEditor()) return entity;
			}
			return null;
		}

		/// <summary>
		/// Ensures the picked entity carries an <see cref="AL_EntityMarker"/> with a stable GUID, then
		/// returns a <see cref="GameEntityRef"/> describing it. Reuses an existing marker if present.
		/// </summary>
		private static GameEntityRef AttachMarker(GameEntity entity)
		{
			if (!entity.HasScriptComponent(MarkerScriptName))
			{
				entity.CreateAndAddScriptComponent(MarkerScriptName, callScriptCallbacks: true);
			}

			AL_EntityMarker marker = entity.GetFirstScriptOfType<AL_EntityMarker>();
			if (marker == null)
			{
				Log($"[EditEntityView] Failed to attach '{MarkerScriptName}' to '{entity.Name}'.", LogLevel.Warning);
				return new GameEntityRef(Guid.NewGuid().ToString("N"), entity.Name);
			}

			if (string.IsNullOrEmpty(marker.RefId))
			{
				marker.RefId = Guid.NewGuid().ToString("N");
				marker.DisplayName = entity.Name;
				Log($"[EditEntityView] Set ID of {marker.DisplayName} to {marker.RefId}", LogLevel.Debug);
			}

			// Force the index to rebuild on next access
			EntityMarkerIndex.Clear();

			return new GameEntityRef(marker.RefId, marker.DisplayName);
		}
	}
}
