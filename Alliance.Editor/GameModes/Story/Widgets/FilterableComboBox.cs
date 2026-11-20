using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Alliance.Editor.GameModes.Story.Widgets
{
	/// <summary>
	/// Free-text box with a filterable, virtualized suggestion list: typing filters and opens the
	/// suggestions, clicking or Enter picks one, Down/Up navigate, Escape closes. Deliberately no
	/// autocomplete - nothing rewrites the typing until an entry is explicitly picked. Values
	/// outside the list stay as typed. Built from scratch (textbox + popup list): the editable
	/// ComboBox machinery is neither virtualized nor tameable enough for the big catalogs
	/// (clips, animations...).
	/// </summary>
	public class FilterableComboBox : UserControl
	{
		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
			"Text", typeof(string), typeof(FilterableComboBox),
			new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextPropertyChanged));

		public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
			"ItemsSource", typeof(IEnumerable), typeof(FilterableComboBox),
			new PropertyMetadata(null, OnItemsSourcePropertyChanged));

		public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
			"IsReadOnly", typeof(bool), typeof(FilterableComboBox), new PropertyMetadata(false, OnIsReadOnlyPropertyChanged));

		private readonly TextBox _textBox = new TextBox { VerticalContentAlignment = VerticalAlignment.Center };
		private readonly Button _toggleButton = CreateToggleButton();
		private readonly ListBox _listBox = new ListBox
		{
			MaxHeight = 220,
			Focusable = false
		};
		private readonly Popup _popup = new Popup { AllowsTransparency = true, StaysOpen = false, PlacementTarget = null };
		// Per-control view over the (possibly shared) source - never filter the shared default view.
		private ListCollectionView _view;
		private bool _suppressAutoOpen;

		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public IEnumerable ItemsSource
		{
			get => (IEnumerable)GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		public bool IsReadOnly
		{
			get => (bool)GetValue(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		public FilterableComboBox()
		{
			// Virtualized suggestion list - the big catalogs (clips, animations) have thousands
			// of entries and a plain ItemsControl stack panel stalls on them.
			VirtualizingStackPanel.SetIsVirtualizing(_listBox, true);
			VirtualizingStackPanel.SetVirtualizationMode(_listBox, VirtualizationMode.Recycling);
			ScrollViewer.SetCanContentScroll(_listBox, true);
			// Items must not take focus: clicking one would move focus out of the text box,
			// whose LostKeyboardFocus handler would close the popup before the click lands.
			Style itemStyle = new Style(typeof(ListBoxItem));
			itemStyle.Setters.Add(new Setter(ListBoxItem.FocusableProperty, false));
			_listBox.ItemContainerStyle = itemStyle;

			_textBox.TextChanged += OnTextBoxTextChanged;
			_textBox.KeyDown += OnTextBoxKeyDown;
			_textBox.PreviewMouseLeftButtonUp += (_, __) => ToggleDropDown();
			// Leaving the field (or the inspector switching its object) must close the suggestions -
			// the popup's outside-click capture misses keyboard-driven context changes.
			_textBox.LostKeyboardFocus += (s, e) =>
			{
				if (!IsDescendantOf(e.NewFocus as DependencyObject, _popup.Child)) _popup.IsOpen = false;
			};
			DataContextChanged += (_, __) => _popup.IsOpen = false;
			Unloaded += (_, __) => _popup.IsOpen = false;

			_toggleButton.Click += (_, __) => ToggleDropDown();

			_listBox.MouseLeftButtonUp += OnListBoxClick;

			Border border = new Border
			{
				BorderBrush = new SolidColorBrush(Color.FromRgb(0x82, 0x87, 0x90)),
				BorderThickness = new Thickness(1),
				Background = Brushes.White,
				Focusable = false,
				Child = _listBox
			};
			_popup.Child = border;
			_popup.Closed += (_, __) => ApplyFilter("");

			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			Grid.SetColumn(_textBox, 0);
			Grid.SetColumn(_toggleButton, 1);
			grid.Children.Add(_textBox);
			grid.Children.Add(_toggleButton);
			Content = grid;

			Loaded += (_, __) => _popup.PlacementTarget = _textBox;
		}

		/// <summary>Flat toggle button mimicking the standard ComboBox arrow plate.</summary>
		private static Button CreateToggleButton()
		{
			Path arrow = new Path
			{
				Data = Geometry.Parse("M 0 0 L 4 4 L 8 0 Z"),
				Fill = new SolidColorBrush(Color.FromRgb(0x59, 0x59, 0x59)),
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			LinearGradientBrush plate = new LinearGradientBrush
			{
				StartPoint = new Point(0, 0),
				EndPoint = new Point(0, 1),
				GradientStops =
				{
					new GradientStop(Color.FromRgb(0xFF, 0xFF, 0xFF), 0),
					new GradientStop(Color.FromRgb(0xE9, 0xE9, 0xE9), 1)
				}
			};
			plate.Freeze();
			return new Button
			{
				Content = arrow,
				Focusable = false,
				Width = 20,
				Background = plate,
				BorderBrush = new SolidColorBrush(Color.FromRgb(0x82, 0x87, 0x90)),
				BorderThickness = new Thickness(1),
				Padding = new Thickness(0)
			};
		}

		private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			FilterableComboBox control = (FilterableComboBox)d;
			string text = e.NewValue as string ?? "";
			if (!string.Equals(control._textBox.Text, text, StringComparison.Ordinal))
			{
				control._suppressAutoOpen = true;
				control._textBox.Text = text;
				control._suppressAutoOpen = false;
			}
		}

		private static void OnIsReadOnlyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			FilterableComboBox control = (FilterableComboBox)d;
			control._textBox.IsReadOnly = (bool)e.NewValue;
		}

		private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			FilterableComboBox control = (FilterableComboBox)d;
			IEnumerable source = e.NewValue as IEnumerable;
			IList list = source as IList;
			if (list == null && source != null) list = source.Cast<object>().ToList();
			control._view = list != null ? new ListCollectionView(list) : null;
			control._listBox.ItemsSource = control._view;
			control.ApplyFilter("");
		}

		private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			Text = _textBox.Text;
			ApplyFilter(_textBox.Text ?? "");

			// Typing opens the suggestions immediately; the popup never takes focus so the caret
			// stays in the text box and nothing rewrites the typing.
			if (!_suppressAutoOpen && !IsReadOnly && _textBox.IsKeyboardFocusWithin && !_popup.IsOpen)
			{
				OpenDropDown();
			}
		}

		private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Down)
			{
				if (!_popup.IsOpen) OpenDropDown();
				else MoveSelection(1);
				e.Handled = true;
			}
			else if (e.Key == Key.Up && _popup.IsOpen)
			{
				MoveSelection(-1);
				e.Handled = true;
			}
			else if (e.Key == Key.Enter && _popup.IsOpen)
			{
				PickSelected();
				e.Handled = true;
			}
			else if (e.Key == Key.Escape && _popup.IsOpen)
			{
				_popup.IsOpen = false;
				e.Handled = true;
			}
		}

		private void OnListBoxClick(object sender, MouseButtonEventArgs e)
		{
			if (e.OriginalSource is not DependencyObject source) return;
			if (IsOverScrollBar(source)) return;
			// Pick the item that was actually clicked - walk up to its container. (The ListBox
			// selection can lag behind the click with recycled containers; the container's own
			// content is always the entry under the cursor.)
			for (DependencyObject node = source; node != null; node = System.Windows.Media.VisualTreeHelper.GetParent(node))
			{
				if (node is ListBoxItem clicked)
				{
					PickItem(clicked.Content);
					return;
				}
			}
		}

		private static bool IsOverScrollBar(DependencyObject source)
		{
			for (DependencyObject d = source; d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
			{
				if (d is ScrollBar) return true;
			}
			return false;
		}

		private static bool IsDescendantOf(DependencyObject node, DependencyObject root)
		{
			if (root == null) return false;
			for (DependencyObject d = node; d != null; d = System.Windows.Media.VisualTreeHelper.GetParent(d))
			{
				if (ReferenceEquals(d, root)) return true;
			}
			return false;
		}

		private void ToggleDropDown()
		{
			if (_popup.IsOpen) _popup.IsOpen = false;
			else OpenDropDown();
		}

		private void OpenDropDown()
		{
			ApplyFilter(_textBox.IsKeyboardFocusWithin ? _textBox.Text ?? "" : "");
			_popup.MinWidth = ActualWidth;
			_popup.IsOpen = true;
			if (_listBox.SelectedIndex < 0 && _listBox.Items.Count > 0) _listBox.SelectedIndex = 0;
		}

		private void MoveSelection(int direction)
		{
			if (_listBox.Items.Count == 0) return;
			int index = _listBox.SelectedIndex + direction;
			_listBox.SelectedIndex = Math.Max(0, Math.Min(_listBox.Items.Count - 1, index));
			_listBox.ScrollIntoView(_listBox.SelectedItem);
		}

		private void PickSelected()
		{
			if (_listBox.SelectedItem != null) PickItem(_listBox.SelectedItem);
			else _popup.IsOpen = false;
		}

		private void PickItem(object item)
		{
			// Writing the picked entry back must not re-open the popup through TextChanged.
			_suppressAutoOpen = true;
			Text = ItemText(item);
			_textBox.Text = Text;
			_textBox.CaretIndex = _textBox.Text.Length;
			_suppressAutoOpen = false;
			// Picking keeps the caret in the field: bindings with UpdateSourceTrigger=LostFocus
			// would sit on the value until the user clicks away - push it now.
			GetBindingExpression(TextProperty)?.UpdateSource();
			_popup.IsOpen = false;
		}

		private void ApplyFilter(string filter)
		{
			if (_view == null) return;
			_view.Filter = filter.Length == 0
				? (Predicate<object>)null
				: item => ItemText(item).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string ItemText(object item) => item?.ToString() ?? "";
	}
}
