using TaleWorlds.Library;

namespace Alliance.Common.Core.UI.VM.Options
{
	public class OptionCategoryVM : ViewModel
	{
		private string _name;
		private bool _isExpanded;
		private MBBindingList<OptionVM> _options;

		[DataSourceProperty]
		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChangedWithValue(value, nameof(Name));
				}
			}
		}

		[DataSourceProperty]
		public bool IsExpanded
		{
			get => _isExpanded;
			set
			{
				if (_isExpanded != value)
				{
					_isExpanded = value;
					OnPropertyChangedWithValue(value, nameof(IsExpanded));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<OptionVM> Options
		{
			get => _options;
			set
			{
				if (_options != value)
				{
					_options = value;
					OnPropertyChangedWithValue(value, nameof(Options));
				}
			}
		}

		public OptionCategoryVM(string name, bool startExpanded = false)
		{
			Name = name;
			IsExpanded = startExpanded;
			Options = new MBBindingList<OptionVM>();
		}

		public void ToggleExpanded()
		{
			IsExpanded = !IsExpanded;
		}
	}
}