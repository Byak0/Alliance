using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM representing a SerializableZone.
	/// </summary>
	public class ZoneViewModel : INotifyPropertyChanged
	{
		private SerializableZone _zone;
		private FieldViewModel _fieldViewModel;

		public ICommand EditZoneCommand { get; }

		public ZoneViewModel(SerializableZone zone, FieldInfo fieldInfo, FieldViewModel fieldViewModel)
		{
			_fieldViewModel = fieldViewModel;
			_zone = zone;
			if (_zone == null)
			{
				_zone = new SerializableZone();
				fieldInfo.SetValue(_fieldViewModel.parentViewModel.Object, _zone);
			}
			EditZoneCommand = new RelayCommand(_ => EditZone(fieldInfo));

			// Register the zone with EditZoneView for display
			string zoneName = ScenarioEditorHelper.GetItemDisplayName(_fieldViewModel.parentViewModel.Object) + " - " + _fieldViewModel.Label;
			EditorToolsManager.AddZoneToEditor(_zone, zoneName, () =>
			{
				// Refresh UI when the zone is updated
				OnPropertyChanged(nameof(X));
				OnPropertyChanged(nameof(Y));
				OnPropertyChanged(nameof(Z));
				OnPropertyChanged(nameof(Radius));
			});
		}

		public void EditZone(FieldInfo fieldInfo)
		{
			if (fieldInfo.FieldType != typeof(SerializableZone)) return;

			// Set this zone as the editable one
			EditorToolsManager.SetEditableZone(_zone);
		}

		public float X
		{
			get => _zone.X;
			set
			{
				if (_zone.X != value)
				{
					_zone.X = value;
					OnPropertyChanged(nameof(X));
				}
			}
		}

		public float Y
		{
			get => _zone.Y;
			set
			{
				if (_zone.Y != value)
				{
					_zone.Y = value;
					OnPropertyChanged(nameof(Y));
				}
			}
		}

		public float Z
		{
			get => _zone.Z;
			set
			{
				if (_zone.Z != value)
				{
					_zone.Z = value;
					OnPropertyChanged(nameof(Z));
				}
			}
		}

		public float Radius
		{
			get => _zone.Radius;
			set
			{
				if (_zone.Radius != value)
				{
					_zone.Radius = value;
					OnPropertyChanged(nameof(Radius));
				}
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
		}
	}
}
