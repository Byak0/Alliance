using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM wrapping a <see cref="Zone"/> for in-editor editing. Hosts a nested <see cref="ObjectEditorViewModel"/>
	/// for the Anchor/Shape pickers, exposes a convenience Radius (for circle shapes), and provides the
	/// "Edit Zone" command that activates click-placement on the scene via <see cref="EditZoneView"/>.
	/// </summary>
	public class ZoneViewModel : INotifyPropertyChanged
	{
		private readonly Zone _zone;
		private readonly FieldViewModel _fieldViewModel;
		private WeakGameEntity _gameEntity => _fieldViewModel?.parentViewModel?.GameEntity ?? WeakGameEntity.Invalid;

		/// <summary>Nested editor for the zone's fields (Anchor, Shape). Position is hidden (rendered inline by this VM).</summary>
		public ObjectEditorViewModel ZoneEditor { get; }

		public ICommand EditZoneCommand { get; }

		public ZoneViewModel(Zone zone, FieldInfo zoneFieldInfo, FieldViewModel fieldViewModel)
		{
			_fieldViewModel = fieldViewModel;
			_zone = zone;
			if (_zone == null)
			{
				_zone = new Zone();
				// Write back into the owning literal or named-zone definition if a field info is provided.
				zoneFieldInfo?.SetValue(_fieldViewModel.parentViewModel.Object, _zone);
			}

			// In the editor, host-relative anchors resolve against the edited object's GameEntity.
			_zone.HostEntity = _gameEntity;

			ZoneEditor = new ObjectEditorViewModel(_zone, _fieldViewModel, _fieldViewModel.scenarioEditorViewModel, "", _gameEntity);
			// Position is rendered inline by this view model's X/Y/Z properties.
			ZoneEditor.HiddenFieldNames = new HashSet<string> { nameof(Zone.Position) };

			EditZoneCommand = new RelayCommand(_ => EditZone());
			ZoneEditor?.RefreshFields();

			// Register the zone with EditZoneView for in-scene display.
			string zoneName = ScenarioEditorHelper.GetItemDisplayName(_fieldViewModel.parentViewModel.Object) + " - " + _fieldViewModel.Label;
			EditorToolsManager.AddZoneToEditor(_zone, zoneName, () =>
			{
				// Refresh UI when the zone is updated from the scene (click placement / wheel).
				ZoneEditor?.RefreshFields();
				OnPropertyChanged(nameof(Radius));
			});
		}

		public void EditZone()
		{
			EditorToolsManager.SetEditableZone(_zone);
		}

		/// <summary>Convenience radius for circle shapes (used by the mouse-wheel handler in EditZoneView).</summary>
		public float Radius
		{
			get => (_zone.Shape as CircleShape)?.Radius ?? 0f;
			set
			{
				if (_zone.Shape is CircleShape circle && circle.Radius != value)
				{
					circle.Radius = value;
					OnPropertyChanged(nameof(Radius));
				}
			}
		}

		public float PositionX
		{
			get => _zone.Position.X;
			set
			{
				_zone.Position = new Vec3(value, _zone.Position.Y, _zone.Position.Z);
				OnPropertyChanged(nameof(PositionX));
			}
		}

		public float PositionY
		{
			get => _zone.Position.Y;
			set
			{
				_zone.Position = new Vec3(_zone.Position.X, value, _zone.Position.Z);
				OnPropertyChanged(nameof(PositionY));
			}
		}

		public float PositionZ
		{
			get => _zone.Position.Z;
			set
			{
				_zone.Position = new Vec3(_zone.Position.X, _zone.Position.Y, value);
				OnPropertyChanged(nameof(PositionZ));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		internal void Close()
		{
			EditorToolsManager.RemoveZoneFromEditor(_zone);
			ZoneEditor?.Close();
		}
	}
}
