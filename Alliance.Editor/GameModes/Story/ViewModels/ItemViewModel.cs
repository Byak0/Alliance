using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM representing a single item in a collection.
	/// </summary>
	public class ItemViewModel : INotifyPropertyChanged
	{
		public object Item { get; private set; }

		private FieldViewModel _fieldViewModel;
		private string _displayName;
		private bool _isPopupOpen;
		private bool _isDropTarget;

		public string DisplayName
		{
			get => _displayName;
			private set
			{
				if (_displayName != value)
				{
					_displayName = value;
					OnPropertyChanged(nameof(DisplayName));
				}
			}
		}

		public bool IsPopupOpen
		{
			get => _isPopupOpen;
			set
			{
				if (_isPopupOpen != value)
				{
					_isPopupOpen = value;
					OnPropertyChanged(nameof(IsPopupOpen));
				}
			}
		}

		public bool IsDropTarget
		{
			get => _isDropTarget;
			set
			{
				if (_isDropTarget != value)
				{
					_isDropTarget = value;
					OnPropertyChanged(nameof(IsDropTarget));
				}
			}
		}

		public ICommand EditCommand { get; }
		public ICommand DeleteCommand { get; }

		public ItemViewModel(object item, FieldViewModel parentViewModel)
		{
			Item = item;
			_fieldViewModel = parentViewModel;
			if (_fieldViewModel.scenarioEditorViewModel != null)
			{
				_fieldViewModel.scenarioEditorViewModel.OnLanguageChange += UpdateDisplayName;
			}

			DisplayName = ScenarioEditorHelper.GetItemDisplayName(item, _fieldViewModel.parentViewModel.SelectedLanguage);

			EditCommand = new RelayCommand(_ => _fieldViewModel.EditObject(Item, this));
			DeleteCommand = new RelayCommand(_ => _fieldViewModel.DeleteItem(this));
		}

		public void UpdateDisplayName(object sender = null, EventArgs args = null)
		{
			DisplayName = ScenarioEditorHelper.GetItemDisplayName(Item, _fieldViewModel.parentViewModel.SelectedLanguage);
		}

		public void ReplaceItem(object newItem)
		{
			Item = newItem;
			UpdateDisplayName();
		}

		public void OnClose()
		{
			UpdateDisplayName();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
