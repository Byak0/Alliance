using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.GameModes.Story.Views
{
	/// <summary>
	/// Logique d'interaction pour ObjectEditorContentControl.xaml
	/// </summary>
	public partial class ObjectEditorContentControl : UserControl
	{
		public ObjectEditorContentControl()
		{
			InitializeComponent();

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		}
	}
}
