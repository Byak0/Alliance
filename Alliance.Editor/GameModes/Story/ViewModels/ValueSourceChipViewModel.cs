using Alliance.Common.GameModes.Story.Utilities;
using System.ComponentModel;
using System.Windows.Input;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// Read-only clickable presentation of a <c>ValueSource&lt;T&gt;</c>. Its text is the expression's
	/// PhrasePreview; editing always happens in <see cref="Views.ValueSourceEditorPopup"/>.
	/// </summary>
	public sealed class ValueSourceChipViewModel : INotifyPropertyChanged
	{
		private readonly FieldViewModel _field;

		public string DisplayText
		{
			get
			{
				object source = _field.FieldValue;
				if (source == null) return "set value";

				string display = ScenarioEditorHelper.GetItemDisplayName(
					source,
					_field.parentViewModel?.SelectedLanguage ?? "English");
				return string.IsNullOrWhiteSpace(display) ? "set value" : display;
			}
		}

		public string ToolTip => string.IsNullOrWhiteSpace(_field.Tooltip)
			? "Choose a literal, variable, or function."
			: _field.Tooltip;

		public ICommand OpenCommand { get; }

		public ValueSourceChipViewModel(FieldViewModel field)
		{
			_field = field;
			OpenCommand = new RelayCommand(_ => _field.OpenValueSourceEditor());
		}

		public void Refresh()
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayText)));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolTip)));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
