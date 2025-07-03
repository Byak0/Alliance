using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using System;

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

		public static void OpenPlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback)
		{
			EditorTools?.OpenPlayerSpawnMenu(playerSpawnMenu, onCloseCallback);
		}

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
