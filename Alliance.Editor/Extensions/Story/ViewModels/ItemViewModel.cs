using Alliance.Editor.Extensions.Story.Utilities;
using System.ComponentModel;
using System.Windows.Input;

namespace Alliance.Editor.Extensions.Story.ViewModels
{
	public class ItemViewModel : INotifyPropertyChanged
	{
		public object Item { get; }
		private string _displayName;
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

		public ICommand EditCommand { get; }
		public ICommand DeleteCommand { get; }

		public ItemViewModel(object item, FieldViewModel parentViewModel)
		{
			Item = item;
			DisplayName = ScenarioEditorHelper.GetItemDisplayName(item);

			EditCommand = new RelayCommand(_ => parentViewModel.EditObject(Item, this));
			DeleteCommand = new RelayCommand(_ => parentViewModel.DeleteItem(this));
		}

		public void UpdateDisplayName()
		{
			DisplayName = ScenarioEditorHelper.GetItemDisplayName(Item);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
