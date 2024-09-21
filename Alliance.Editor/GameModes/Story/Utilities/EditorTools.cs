using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Editor.GameModes.Story.ViewModels;
using Alliance.Editor.GameModes.Story.Views;
using System;

namespace Alliance.Editor.GameModes.Story.Utilities
{
	public class EditorTools : IEditorTools
	{
		private ObjectEditorWindow _objectEditorWindow;

		public void AddZoneToEditor(SerializableZone zone, string zoneName, Action onEditCallback)
		{
			EditZoneView.AddZone(zone, zoneName, onEditCallback);
		}

		public void RemoveZoneFromEditor(SerializableZone zone)
		{
			EditZoneView.RemoveZone(zone);
		}

		public void ClearZones()
		{
			EditZoneView.ClearZones();
		}

		public void SetEditableZone(SerializableZone zone)
		{
			EditZoneView.SetEditableZone(zone);
		}

		/// <summary>
		/// Open the ObjectEditor window to edit the given object.
		/// </summary>
		public void OpenEditor(object obj, Action<object> onCloseCallback)
		{
			// Check if the window is already opened
			if (_objectEditorWindow == null || !_objectEditorWindow.IsLoaded)
			{
				// Create and show the editor window without blocking the scene
				_objectEditorWindow = new ObjectEditorWindow(obj);
				_objectEditorWindow.Show();

				// Handle the window's Closed event to return the modified object
				_objectEditorWindow.Closed += (s, e) =>
				{
					// Retrieve the modified object from the ViewModel
					object _modifiedObject = (_objectEditorWindow.DataContext as ObjectEditorViewModel)?.Object;

					// Call the callback with the modified object
					onCloseCallback?.Invoke(_modifiedObject);

					// Dispose of the window instance
					_objectEditorWindow = null;
				};
			}
			else
			{
				// If the window is already open, just bring it to focus
				_objectEditorWindow.Focus();
			}
		}
	}
}
