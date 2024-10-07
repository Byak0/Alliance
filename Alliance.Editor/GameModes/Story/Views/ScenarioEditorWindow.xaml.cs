using Alliance.Common.GameModes.Story.Models;
using Alliance.Editor.GameModes.Story.ViewModels;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.GameModes.Story.Views
{
	public partial class ScenarioEditorWindow : Window
	{
		public ScenarioEditorWindow(Scenario scenario)
		{
			InitializeComponent();

			DataContext = new ScenarioEditorViewModel(scenario);

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (DataContext != null && !(DataContext as ScenarioEditorViewModel).ConfirmUnsavedChanges())
			{
				e.Cancel = true;
			}
		}
	}
}
