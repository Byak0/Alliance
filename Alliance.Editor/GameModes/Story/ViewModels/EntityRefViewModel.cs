using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.ComponentModel;
using System.Windows.Input;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM wrapping a <see cref="GameEntityRef"/> for in-editor editing. Exposes a "Select entity on map"
	/// command that activates click-pick in the scene via <see cref="EditEntityView"/> (through
	/// <see cref="EditorToolsManager"/>), and a read-only display of the currently referenced entity.
	/// </summary>
	public class EntityRefViewModel : INotifyPropertyChanged
	{
		private readonly GameEntityRef _ref;
		private readonly FieldViewModel _fieldViewModel;

		public ICommand PickEntityCommand { get; }

		public EntityRefViewModel(GameEntityRef gameEntityRef, FieldViewModel fieldViewModel)
		{
			_fieldViewModel = fieldViewModel;
			_ref = gameEntityRef;
			if (_ref == null)
			{
				_ref = new GameEntityRef();
				// Write back into the owning literal so the value is not lost.
				_fieldViewModel?.FieldInfo?.SetValue(_fieldViewModel.parentViewModel?.Object, _ref);
			}

			PickEntityCommand = new RelayCommand(_ => PickEntity());
		}

		public string DisplayName => string.IsNullOrEmpty(_ref?.DisplayName)
			? "(not set)"
			: _ref.DisplayName;

		public string RefId => _ref?.RefId ?? "";

		public void PickEntity()
		{
			EditorToolsManager.BeginEntityPick(gameRef =>
			{
				_ref.RefId = gameRef.RefId;
				_ref.DisplayName = gameRef.DisplayName;
				OnPropertyChanged(nameof(DisplayName));
				OnPropertyChanged(nameof(RefId));
			});
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		internal void Close()
		{
		}
	}
}
