using Alliance.Common.GameModes.Story.Models;
using Alliance.Editor.Extensions.Story.ViewModels;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.Extensions.Story.Views
{
	public partial class ScenarioEditorWindow : Window
	{
		private readonly ScenarioEditorViewModel _viewModel;

		public ScenarioEditorWindow(Scenario scenario)
		{
			InitializeComponent();

			_viewModel = new ScenarioEditorViewModel(scenario);
			DataContext = _viewModel;

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		}

		private void ObjectEditorContentControl_Loaded(object sender, RoutedEventArgs e)
		{
		}
	}
}
