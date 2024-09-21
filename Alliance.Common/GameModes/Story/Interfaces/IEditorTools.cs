using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Common.GameModes.Story.Interfaces
{
	public interface IEditorTools
	{
		public void AddZoneToEditor(SerializableZone zone, string zoneName, Action onEditCallback);
		public void RemoveZoneFromEditor(SerializableZone zone);
		public void SetEditableZone(SerializableZone zone);
		public void ClearZones();
		public void OpenEditor(object obj, Action<object> onCloseCallback);
	}
}
