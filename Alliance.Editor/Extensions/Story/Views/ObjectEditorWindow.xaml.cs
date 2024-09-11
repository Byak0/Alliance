using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.Extensions.Story.Views
{
	public partial class ObjectEditorWindow : Window
	{
		public ObjectEditorWindow()
		{
			InitializeComponent();

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		}
	}
}
