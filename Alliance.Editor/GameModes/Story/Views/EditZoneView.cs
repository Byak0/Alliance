using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Editor.GameModes.Story.Views
{
	public static class EditZoneView
	{
		private static SceneView _sceneView;

		private static readonly uint _editableColor = Color.White.ToUnsignedInteger();
		private static readonly List<uint> _colorList = new List<uint>() {
			new Color(1f, 0f, 0f).ToUnsignedInteger(),
			new Color(0f, 1f, 0f).ToUnsignedInteger(),
			new Color(1f, 1f, 0f).ToUnsignedInteger(),
			new Color(0f, 1f, 1f).ToUnsignedInteger(),
			new Color(1f, 0f, 1f).ToUnsignedInteger(),
			new Color(1f, 0.5f, 0f).ToUnsignedInteger()
		};

		private static bool _rightClickHeld = false;
		private static float _rightClickHoldTime = 0f;

		// Store zones with their names and edit callbacks
		private static Dictionary<SerializableZone, (string zoneName, Action onEditCallback)> _zones = new Dictionary<SerializableZone, (string, Action)>();

		// Track the currently editable zone
		private static SerializableZone _editableZone;
		public static Action OnEditCallBack;

		// Add a zone with its associated name
		public static void AddZone(SerializableZone zone, string zoneName, Action onEditCallback)
		{
			_sceneView = MBEditor.GetEditorSceneView();

			if (!_zones.ContainsKey(zone))
			{
				_zones.Add(zone, (zoneName, onEditCallback));
			}
		}

		public static void RemoveZone(SerializableZone zone)
		{
			if (_zones.ContainsKey(zone))
			{
				_zones.Remove(zone);
			}
		}

		public static void SetEditableZone(SerializableZone zone)
		{
			if (_zones.ContainsKey(zone))
			{
				_editableZone = zone;
				OnEditCallBack = _zones[zone].onEditCallback;
			}
		}

		public static void ClearZones()
		{
			_zones.Clear();
			_editableZone = null;
			OnEditCallBack = null;
		}

		public static void Tick(float dt)
		{
			int colorIndex = 0;

			// Display all zones
			foreach (var entry in _zones)
			{
				SerializableZone zone = entry.Key;
				bool isEditable = zone == _editableZone;

				Vec3 position;
				if (zone.UseLocalSpace && zone.LocalEntity != null)
				{
					Vec3 entityPosition = zone.LocalEntity.GlobalPosition;
					Vec3 zoneLocalPosition = new Vec3(zone.X, zone.Y, zone.Z);
					position = entityPosition + zoneLocalPosition;
				}
				else
				{
					position = zone.Position;
				}
				uint color = isEditable ? _editableColor : _colorList[colorIndex % _colorList.Count];

				// Render the zone sphere and the associated name
				Debug.RenderDebugSphere(position, zone.Radius, color, true);
				Debug.RenderDebugText3D(position, entry.Value.zoneName, color, -100);

				colorIndex++;
			}

			// Update the editable zone
			if (_editableZone != null)
			{
				HandleZoneEditing(dt);
			}
		}

		private static void HandleZoneEditing(float dt)
		{
			// Track how long the right mouse button is held
			if (Input.IsKeyDown(InputKey.RightMouseButton))
			{
				_rightClickHoldTime += dt;

				// If right-click is held for more than the threshold, treat it as camera movement
				if (_rightClickHoldTime > 0.2f)
				{
					_rightClickHeld = true;
				}
			}
			else if (Input.IsKeyReleased(InputKey.RightMouseButton))
			{
				// If right-click is released and was not held for camera movement, exit zone editing
				if (!_rightClickHeld)
				{
					_editableZone = null;
				}

				// Reset the flags and timer after the release
				_rightClickHeld = false;
				_rightClickHoldTime = 0f;
			}

			// Update the zone's position on click
			if (Input.IsKeyDown(InputKey.LeftMouseButton))
			{
				_sceneView.ProjectedMousePositionOnGround(out var groundPosition, out var groundNormal, true, BodyFlags.BodyOwnerFlora, checkOccludedSurface: true);
				UpdateZonePosition(groundPosition, _editableZone);
				OnEditCallBack?.Invoke();
			}

			// Update the zone's radius on mouse wheel
			if (Input.IsKeyDown(InputKey.MouseScrollDown))
			{
				_editableZone.Radius -= 0.1f;
				OnEditCallBack?.Invoke();
			}
			else if (Input.IsKeyDown(InputKey.MouseScrollUp))
			{
				_editableZone.Radius += 0.1f;
				OnEditCallBack?.Invoke();
			}
		}

		private static void UpdateZonePosition(Vec3 groundPosition, SerializableZone zone)
		{
			if (zone.UseLocalSpace && zone.LocalEntity != null)
			{
				// Transform the ground position to the local space of the entity
				groundPosition -= zone.LocalEntity.GlobalPosition;
			}
			zone.Position = groundPosition;
			zone.X = groundPosition.x;
			zone.Y = groundPosition.y;
			zone.Z = groundPosition.z;
		}
	}
}
