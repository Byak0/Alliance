using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Views;
using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Editor.GameModes.Story.ViewModels;
using Alliance.Editor.GameModes.Story.Views;
using System;

namespace Alliance.Editor.GameModes.Story.Utilities
{
	/// <summary>
	/// Some tools for the modding kit. Allow editing zones, player spawn menus, and any simple object.
	/// Mostly used with the Scenario Editor and AL_TriggerAction script.
	/// </summary>
	public class EditorTools : IEditorTools
	{
		private ObjectEditorWindow _objectEditorWindow;
		private PlayerSpawnMenuView _playerSpawnMenuView;

		public EditorTools()
		{
			// Initialize the PlayerSpawnMenuView
			_playerSpawnMenuView = new PlayerSpawnMenuView();
			_playerSpawnMenuView.OnBehaviorInitialize();
		}

		public void Tick(float dt)
		{
			_playerSpawnMenuView.OnMissionTick(dt);
			EditZoneView.Tick(dt);
			EditEntityView.Tick(dt);
			EditFrameView.Tick(dt);
		}

		public void OpenPlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback)
		{
			_playerSpawnMenuView.OpenMenu(playerSpawnMenu, onCloseCallback, true);
		}

		public void AddZoneToEditor(Zone zone, string zoneName, Action onEditCallback)
		{
			EditZoneView.AddZone(zone, zoneName, onEditCallback);
		}

		public void RemoveZoneFromEditor(Zone zone)
		{
			EditZoneView.RemoveZone(zone);
		}

		public void ClearZones()
		{
			EditZoneView.ClearZones();
		}

		public void SetEditableZone(Zone zone)
		{
			EditZoneView.SetEditableZone(zone);
		}

		/// <summary>
		/// Enters entity pick mode: the next entity selected in the editor viewport is captured into a
		/// <see cref="GameEntityRef"/> (with an attached <c>AL_EntityMarker</c> GUID) and passed to the callback.
		/// </summary>
		public void BeginEntityPick(Action<GameEntityRef> onPicked)
		{
			EditEntityView.BeginPick(onPicked);
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
