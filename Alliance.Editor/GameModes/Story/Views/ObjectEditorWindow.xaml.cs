using Alliance.Editor.GameModes.Story.ViewModels;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TaleWorlds.Engine;

namespace Alliance.Editor.GameModes.Story.Views
{
	public partial class ObjectEditorWindow : Window
	{
		public ObjectEditorWindow(object obj, ScenarioEditorViewModel parentViewModel = null, string title = "Object Editor", GameEntity gameEntity = null)
		{
			InitializeComponent();

			// Pass the object to the ObjectEditorViewModel
			DataContext = new ObjectEditorViewModel(obj, parentViewModel, title, gameEntity);

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

			Topmost = true; // Keep it on top
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (DataContext != null && DataContext is ObjectEditorViewModel objectEditorVM)
			{
				objectEditorVM.Close();
			}
		}

		private void Window_Loaded(object sender, EventArgs e)
		{
			// Get the current mouse cursor position
			var cursorPosition = System.Windows.Forms.Cursor.Position;
			var workingArea = SystemParameters.WorkArea;

			// Set the window's Left and Top properties based on the cursor position
			Left = cursorPosition.X;
			Top = cursorPosition.Y;

			// Adjust window's position if it exceeds screen boundaries
			if (Left + ActualWidth > workingArea.Right)
			{
				Left = workingArea.Right - ActualWidth;
			}
			if (Top + ActualHeight > workingArea.Bottom)
			{
				Top = workingArea.Bottom - ActualHeight;
			}
			if (Left < workingArea.Left)
			{
				Left = workingArea.Left;
			}
			if (Top < workingArea.Top)
			{
				Top = workingArea.Top;
			}
		}
	}
}
