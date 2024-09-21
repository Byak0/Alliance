using Alliance.Editor.GameModes.Story.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.GameModes.Story.Views
{
	/// <summary>
	/// Logique d'interaction pour TypeSelectionForm.xaml
	/// </summary>
	public partial class TypeSelectionForm : Window
	{
		public Type SelectedType => (DataContext as TypeSelectionViewModel)?.SelectedType;

		public TypeSelectionForm()
		{
			InitializeComponent();

			// Disable hardware acceleration for this window to prevent Steam overlay detection
			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

			Topmost = true; // Keep it on top
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Get the current mouse cursor position
			var cursorPosition = Mouse.GetPosition(this);
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

		private void OKCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}
