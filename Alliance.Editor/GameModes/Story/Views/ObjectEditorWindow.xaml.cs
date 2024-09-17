using Alliance.Editor.GameModes.Story.ViewModels;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.GameModes.Story.Views
{
	public partial class ObjectEditorWindow : Window
	{
		public ObjectEditorWindow(object obj, ScenarioEditorViewModel parentViewModel = null, string title = "Object Editor")
		{
			InitializeComponent();

			// Pass the object to the ObjectEditorViewModel
			DataContext = new ObjectEditorViewModel(obj, parentViewModel, title);

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		}
	}
}
