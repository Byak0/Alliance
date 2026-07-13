using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	public class FieldCategoryViewModel : INotifyPropertyChanged
	{
		private string _name;
		private bool _isExpanded;
		private ObservableCollection<FieldViewModel> _fields;

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged(nameof(Name));
				}
			}
		}

		public bool IsExpanded
		{
			get => _isExpanded;
			set
			{
				if (_isExpanded != value)
				{
					_isExpanded = value;
					OnPropertyChanged(nameof(IsExpanded));
				}
			}
		}

		public ObservableCollection<FieldViewModel> Fields
		{
			get => _fields;
			set
			{
				if (_fields != value)
				{
					_fields = value;
					OnPropertyChanged(nameof(Fields));
				}
			}
		}

		public FieldCategoryViewModel(string name, bool startExpanded = false)
		{
			Name = name;
			IsExpanded = startExpanded;
			Fields = new ObservableCollection<FieldViewModel>();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
