using Alliance.Editor.GameModes.Story.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.GameModes.Story.Views
{
	/// <summary>Modal-like editor window for one recursive ValueSource expression slot.</summary>
	public partial class ValueSourceEditorPopup : Window
	{
		public ValueSourceEditorPopup(FieldViewModel field)
		{
			InitializeComponent();
			DataContext = new ValueSourceEditorViewModel(field);

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		}

		/// <summary>Allows the caller to supply a pre-built ViewModel (e.g. for list items).</summary>
		public ValueSourceEditorPopup(ValueSourceEditorViewModel viewModel, FieldViewModel _)
		{
			InitializeComponent();
			DataContext = viewModel;
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
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
