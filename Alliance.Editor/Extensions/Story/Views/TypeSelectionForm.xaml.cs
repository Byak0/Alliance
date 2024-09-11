using Alliance.Editor.Extensions.Story.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Alliance.Editor.Extensions.Story.Views
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
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Get the current mouse cursor position
			var cursorPosition = Mouse.GetPosition(this);

			// Set the window's Left and Top properties based on the cursor position
			Left = cursorPosition.X;
			Top = cursorPosition.Y;
		}

		private void OKCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}
