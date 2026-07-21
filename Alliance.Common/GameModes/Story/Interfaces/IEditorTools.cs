using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Common.GameModes.Story.Interfaces
{
	public interface IEditorTools
	{
		public void Tick(float dt);
		public void OpenPlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback);
		public void AddZoneToEditor(Zone zone, string zoneName, Action onEditCallback);
		public void RemoveZoneFromEditor(Zone zone);
		public void SetEditableZone(Zone zone);
		public void ClearZones();
		public void OpenEditor(object obj, Action<object> onCloseCallback);
	}
}
