using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Manager for the editor tools. Implemented in Editor project.
	/// </summary>
	public static class EditorToolsManager
	{
		public static IEditorTools EditorTools;

		public static void AddZoneToEditor(SerializableZone zone, string zoneName, Action onEditCallback)
		{
			EditorTools?.AddZoneToEditor(zone, zoneName, onEditCallback);
		}

		public static void RemoveZoneFromEditor(SerializableZone zone)
		{
			EditorTools?.RemoveZoneFromEditor(zone);
		}

		public static void SetEditableZone(SerializableZone zone)
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
	}
}
