using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Editor.Extensions.Cinematics.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Alliance.Common.Extensions.Cinematics.Models;

namespace Alliance.Editor.Extensions.Cinematics.Views
{
	/// <summary>
	/// The single, dedicated cinematic editor. Opened from both Scenario.Cinematics and
	/// PlayCinematicAction. Hosts transport/scrubber, a draggable camera-keyframe timeline lane, an inspector,
	/// capture, and preview (rendered by <see cref="EditorTools"/>).
	/// </summary>
	public partial class CinematicEditorWindow : Window
	{
		private readonly CinematicTimelineVM _vm;

		public CinematicEditorWindow(Cinematic cinematic, Action<Cinematic> onClosed)
		{
			InitializeComponent();
			_vm = new CinematicTimelineVM(cinematic, onClosed);
			DataContext = _vm;

			RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
			Topmost = true;
		}

		private void Window_Loaded(object sender, EventArgs e)
		{
			var cursor = System.Windows.Forms.Cursor.Position;
			var work = SystemParameters.WorkArea;
			Left = cursor.X;
			Top = cursor.Y;
			if (Left + ActualWidth > work.Right) Left = work.Right - ActualWidth;
			if (Top + ActualHeight > work.Bottom) Top = work.Bottom - ActualHeight;
			if (Left < work.Left) Left = work.Left;
			if (Top < work.Top) Top = work.Top;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => _vm.OnExternalClose();

		// Keyframe selection + drag (change time)
		private void Marker_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			switch (fe?.DataContext)
			{
				case CameraKeyframeVM cvm:
					_vm.SelectedKeyframe = cvm;
					break;
				case KeyframeMarkerVM gvm:
					gvm.SelectCommand?.Execute(null);
					break;
			}
		}

		private void Marker_DragDelta(object sender, DragDeltaEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			float dur = _vm.EffectiveDuration;
			float dt = (float)(e.HorizontalChange / _vm.UsableWidth) * dur;
			switch (fe?.DataContext)
			{
				case CameraKeyframeVM cvm:
					cvm.Time = cvm.Time + dt;
					break;
				case KeyframeMarkerVM gvm:
					gvm.Time = gvm.Time + dt;
					break;
			}
		}

		// Click empty lane to seek
		private void Lane_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (!(sender is FrameworkElement fe)) return;
			Point p = e.GetPosition(fe);
			float t = ((float)p.X - CinematicTimelineVM.TimelinePadding) / _vm.UsableWidth * _vm.EffectiveDuration;
			_vm.CurrentTime = Math.Max(0f, Math.Min(_vm.EffectiveDuration, t));
			if (EditorToolsManager.IsPreviewing) EditorToolsManager.SeekPreview(_vm.CurrentTime);
		}

		// Keep the timeline lane width in sync with the window so keyframes scale with it
		private void Lane_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (_vm != null && sender is FrameworkElement fe)
				_vm.TimelineWidth = (float)fe.ActualWidth;
		}
	}
}
