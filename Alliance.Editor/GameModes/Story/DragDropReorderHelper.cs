using Alliance.Editor.GameModes.Story.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Alliance.Editor.GameModes.Story
{
	public static class DragDropReorderHelper
	{
		public static readonly DependencyProperty IsEnabledProperty =
			DependencyProperty.RegisterAttached(
				"IsEnabled", typeof(bool), typeof(DragDropReorderHelper),
				new PropertyMetadata(false, OnIsEnabledChanged));

		private static readonly DependencyProperty StartPointProperty =
			DependencyProperty.RegisterAttached(
				"StartPoint", typeof(Point), typeof(DragDropReorderHelper),
				new PropertyMetadata(new Point(-1, -1)));

		private static readonly DependencyProperty DragStateProperty =
			DependencyProperty.RegisterAttached(
				"DragState", typeof(DragContext), typeof(DragDropReorderHelper),
				new PropertyMetadata(null));

		public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
		public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

		private static Point GetStartPoint(DependencyObject obj) => (Point)obj.GetValue(StartPointProperty);
		private static void SetStartPoint(DependencyObject obj, Point value) => obj.SetValue(StartPointProperty, value);
		private static DragContext GetDragState(DependencyObject obj) => (DragContext)obj.GetValue(DragStateProperty);
		private static void SetDragState(DependencyObject obj, DragContext value) => obj.SetValue(DragStateProperty, value);

		private class DragContext
		{
			public int FromIndex;
			public bool IsDragging;
		}

		private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is ItemsControl ic)) return;

			if ((bool)e.NewValue)
			{
				ic.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
				ic.PreviewMouseMove += OnPreviewMouseMove;
				ic.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
				ic.MouseLeave += OnMouseLeave;
			}
			else
			{
				ic.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
				ic.PreviewMouseMove -= OnPreviewMouseMove;
				ic.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
				ic.MouseLeave -= OnMouseLeave;
				CleanupState(ic);
			}
		}

		private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var ic = (ItemsControl)sender;
			int index = GetHoveredItemIndex(ic, e.GetPosition(ic));
			if (index < 0) return;

			CleanupState(ic);

			SetStartPoint(ic, e.GetPosition(ic));
			SetDragState(ic, new DragContext { FromIndex = index });
		}

		private static void OnPreviewMouseMove(object sender, MouseEventArgs e)
		{
			var ic = (ItemsControl)sender;
			var state = GetDragState(ic);
			if (state == null) return;

			if (!state.IsDragging)
			{
				if (e.LeftButton != MouseButtonState.Pressed)
				{
					SetDragState(ic, null);
					SetStartPoint(ic, new Point(-1, -1));
					return;
				}

				Point start = GetStartPoint(ic);
				Point current = e.GetPosition(ic);
				double threshold = SystemParameters.MinimumHorizontalDragDistance;

				if (Math.Abs(current.X - start.X) > threshold || Math.Abs(current.Y - start.Y) > threshold)
				{
					state.IsDragging = true;
					SetStartPoint(ic, new Point(-1, -1));
				}
			}
			else
			{
				if (e.LeftButton != MouseButtonState.Pressed)
				{
					EndDrag(ic, state);
					return;
				}

				UpdateDropTarget(ic, e.GetPosition(ic), state.FromIndex);
			}
		}

		private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var ic = (ItemsControl)sender;
			var state = GetDragState(ic);
			if (state == null) return;

			if (state.IsDragging)
			{
				int hoverIndex = GetHoveredItemIndex(ic, e.GetPosition(ic));
				if (hoverIndex >= 0)
				{
					int toIndex = ComputeTargetIndex(state.FromIndex, hoverIndex);
					if (toIndex != state.FromIndex)
					{
						var fieldVM = ic.DataContext as FieldViewModel;
						fieldVM?.MoveItem(state.FromIndex, toIndex);
					}
				}
				EndDrag(ic, state);
				e.Handled = true;
				// Force-release any stale mouse capture (e.g. from the Edit button)
				ic.CaptureMouse();
				ic.ReleaseMouseCapture();
			}
			else
			{
				SetDragState(ic, null);
				SetStartPoint(ic, new Point(-1, -1));
			}
		}

		private static void OnMouseLeave(object sender, MouseEventArgs e)
		{
			var ic = (ItemsControl)sender;
			var state = GetDragState(ic);
			if (state?.IsDragging == true)
				EndDrag(ic, state);
		}

		private static void CleanupState(ItemsControl ic)
		{
			var stale = GetDragState(ic);
			if (stale != null)
			{
				if (stale.IsDragging)
					ClearDropTarget(ic);
				SetDragState(ic, null);
			}
		}

		private static void EndDrag(ItemsControl ic, DragContext state)
		{
			ClearDropTarget(ic);
			SetDragState(ic, null);
		}

		private static int GetHoveredItemIndex(ItemsControl ic, Point pos)
		{
			int count = ic.Items.Count;
			for (int i = 0; i < count; i++)
			{
				var container = ic.ItemContainerGenerator.ContainerFromIndex(i) as UIElement;
				if (container == null) continue;

				Rect bounds = container.TransformToAncestor(ic).TransformBounds(new Rect(container.RenderSize));

				if (pos.X >= bounds.Left && pos.X < bounds.Right &&
					pos.Y >= bounds.Top && pos.Y < bounds.Bottom)
					return i;
			}
			return -1;
		}

		private static int ComputeTargetIndex(int fromIndex, int hoverIndex)
		{
			if (fromIndex < hoverIndex)
				return hoverIndex + 1;
			else
				return hoverIndex;
		}

		private static void UpdateDropTarget(ItemsControl ic, Point mousePos, int fromIndex)
		{
			int hoverIndex = GetHoveredItemIndex(ic, mousePos);
			ClearDropTarget(ic);

			if (hoverIndex < 0) return;
			if (hoverIndex == fromIndex) return;

			if (hoverIndex >= 0 && hoverIndex < ic.Items.Count && ic.Items[hoverIndex] is ItemViewModel vm)
			{
				vm.IsDropTarget = true;
			}
		}

		private static void ClearDropTarget(ItemsControl ic)
		{
			foreach (var item in ic.Items)
			{
				if (item is ItemViewModel vm)
					vm.IsDropTarget = false;
			}
		}
	}
}
