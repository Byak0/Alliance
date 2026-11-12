using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Alliance.Editor.GameModes.Story
{
	/// <summary>
	/// Attached behavior that sizes a ComboBox to fit its widest item (and the current text for
	/// editable combos), clamped to the ComboBox's <see cref="FrameworkElement.MaxWidth"/>.
	/// Works around WPF's unreliable built-in auto-sizing, which leaves some combos too wide and
	/// others clipping their content.
	/// <para>Usage: <c>local:ComboBoxAutoSize.AutoSize="True"</c> alongside a <c>MaxWidth</c> cap.</para>
	/// </summary>
	public static class ComboBoxAutoSize
	{
		public static readonly DependencyProperty AutoSizeProperty =
			DependencyProperty.RegisterAttached(
				"AutoSize",
				typeof(bool),
				typeof(ComboBoxAutoSize),
				new PropertyMetadata(false, OnAutoSizeChanged));

		public static bool GetAutoSize(DependencyObject obj) => (bool)obj.GetValue(AutoSizeProperty);
		public static void SetAutoSize(DependencyObject obj, bool value) => obj.SetValue(AutoSizeProperty, value);

		// Room for the dropdown toggle button and ComboBox padding.
		private const double ExtraWidth = 34d;
		private const double MinAutoWidth = 40d;

		private static void OnAutoSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (!(d is ComboBox cb) || !(bool)e.NewValue) return;

			cb.Loaded += (_, __) => Recompute(cb);
			cb.DataContextChanged += (_, __) => Recompute(cb);

			if (cb.ItemsSource is INotifyCollectionChanged incc)
			{
				incc.CollectionChanged += (_, __) => Recompute(cb);
			}
		}

		private static void Recompute(ComboBox cb)
		{
			if (cb == null) return;

			List<string> candidates = new List<string>();

			IEnumerable source = cb.ItemsSource as IEnumerable;
			if (source != null)
			{
				foreach (object item in source)
				{
					if (item != null) candidates.Add(item.ToString());
				}
			}
			else
			{
				foreach (object item in cb.Items)
				{
					if (item != null) candidates.Add(item.ToString());
				}
			}

			if (cb.IsEditable && !string.IsNullOrEmpty(cb.Text))
			{
				candidates.Add(cb.Text);
			}

			if (candidates.Count == 0) return;

			TextBlock measure = new TextBlock
			{
				FontFamily = cb.FontFamily,
				FontSize = cb.FontSize,
				FontStyle = cb.FontStyle,
				FontWeight = cb.FontWeight,
				FontStretch = cb.FontStretch
			};

			Size infinite = new Size(double.PositiveInfinity, double.PositiveInfinity);
			double maxTextWidth = 0;
			foreach (string text in candidates)
			{
				measure.Text = text;
				measure.Measure(infinite);
				if (measure.DesiredSize.Width > maxTextWidth) maxTextWidth = measure.DesiredSize.Width;
			}

			double desired = maxTextWidth + ExtraWidth;
			double maxWidth = cb.MaxWidth > 0 ? cb.MaxWidth : double.PositiveInfinity;
			cb.Width = Math.Max(MinAutoWidth, Math.Min(desired, maxWidth));
		}
	}
}
