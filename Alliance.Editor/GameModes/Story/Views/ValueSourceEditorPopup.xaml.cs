using Alliance.Editor.GameModes.Story.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace Alliance.Editor.GameModes.Story.Views
{
	/// <summary>Modal-like editor window for one recursive ValueSource expression slot.</summary>
	public partial class ValueSourceEditorPopup : Window
	{
		public ValueSourceEditorPopup(FieldViewModel field)
		{
			InitializeComponent();
			DataContext = new ValueSourceEditorViewModel(field);
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (DataContext is ValueSourceEditorViewModel viewModel)
			{
				viewModel.Close();
			}
		}
	}
}
