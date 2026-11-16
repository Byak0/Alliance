using Alliance.Common.Extensions.Cinematics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Alliance.Editor.GameModes.Story.Widgets
{
	/// <summary>
	/// Editor for a #RRGGBBAA hex color string: live swatch preview, validated text input and a drop-down
	/// popup with a preset palette plus R/G/B/A sliders. Values are normalized through <see cref="HexColor"/>,
	/// so the output string is always safe for Gauntlet's <c>Color.ConvertStringToColor</c>.
	/// </summary>
	public class ColorPickerBox : Control
	{
		private TextBox _textBox;
		private Border _swatch;
		private ToggleButton _dropDownToggle;
		private Popup _popup;
		private ItemsControl _palette;
		private Slider _redSlider;
		private Slider _greenSlider;
		private Slider _blueSlider;
		private Slider _alphaSlider;
		private TextBlock _redValue;
		private TextBlock _greenValue;
		private TextBlock _blueValue;
		private TextBlock _alphaValue;
		private bool _updating;
		private bool _forceSyncTextBox;

		static ColorPickerBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPickerBox), new FrameworkPropertyMetadata(typeof(ColorPickerBox)));
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(nameof(Text), typeof(string), typeof(ColorPickerBox),
				new FrameworkPropertyMetadata(HexColor.Default, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged, CoerceText));

		/// <summary>The hex string being edited, always in canonical #RRGGBBAA form.</summary>
		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		/// <summary>Preset colors offered in the drop-down palette, generated at runtime.</summary>
		public IReadOnlyList<string> Palette { get; } = BuildPalette();

		/// <summary>
		/// Builds the preset palette: classic subtitle colors, then a 12-hue ramp (3 tints each) and a gray ramp.
		/// Generated from HSV so the swatches form an even, familiar rainbow like standard color pickers.
		/// </summary>
		private static string[] BuildPalette()
		{
			var colors = new List<string>
			{
				// Classic subtitle colors first (white, black, yellow)
				"#FFFFFFFF", "#000000FF", "#FFF2C5FF"
			};
			for (int hue = 0; hue < 360; hue += 30)
			{
				colors.Add(HsvToHex(hue, 1f, 0.80f));
				colors.Add(HsvToHex(hue, 1f, 0.55f));
				colors.Add(HsvToHex(hue, 0.55f, 0.35f));
			}
			for (int step = 1; step <= 4; step++)
			{
				byte gray = (byte)(255 * step / 5);
				colors.Add(HexColor.FromBytes(gray, gray, gray, 255));
			}
			return colors.ToArray();
		}

		private static string HsvToHex(float hue, float saturation, float value)
		{
			float c = value * saturation;
			float x = c * (1f - Math.Abs(hue / 60f % 2f - 1f));
			float m = value - c;
			float r = 0f, g = 0f, b = 0f;
			if (hue < 60f) { r = c; g = x; }
			else if (hue < 120f) { r = x; g = c; }
			else if (hue < 180f) { g = c; b = x; }
			else if (hue < 240f) { g = x; b = c; }
			else if (hue < 300f) { r = x; b = c; }
			else { r = c; b = x; }
			return HexColor.FromBytes((byte)((r + m) * 255f + 0.5f), (byte)((g + m) * 255f + 0.5f), (byte)((b + m) * 255f + 0.5f), 255);
		}

		private static object CoerceText(DependencyObject d, object baseValue)
		{
			return HexColor.Normalize(baseValue as string);
		}

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((ColorPickerBox)d).RefreshDisplay();
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UnwireTemplateParts();

			_textBox = GetTemplateChild("PART_TextBox") as TextBox;
			_swatch = GetTemplateChild("PART_Swatch") as Border;
			_dropDownToggle = GetTemplateChild("PART_DropDownToggle") as ToggleButton;
			_popup = GetTemplateChild("PART_Popup") as Popup;
			_palette = GetTemplateChild("PART_Palette") as ItemsControl;
			_redSlider = GetTemplateChild("PART_RedSlider") as Slider;
			_greenSlider = GetTemplateChild("PART_GreenSlider") as Slider;
			_blueSlider = GetTemplateChild("PART_BlueSlider") as Slider;
			_alphaSlider = GetTemplateChild("PART_AlphaSlider") as Slider;
			_redValue = GetTemplateChild("PART_RedValue") as TextBlock;
			_greenValue = GetTemplateChild("PART_GreenValue") as TextBlock;
			_blueValue = GetTemplateChild("PART_BlueValue") as TextBlock;
			_alphaValue = GetTemplateChild("PART_AlphaValue") as TextBlock;

			if (_textBox != null)
			{
				_textBox.LostFocus += OnTextBoxLostFocus;
				_textBox.PreviewKeyDown += OnTextBoxKeyDown;
				_textBox.TextChanged += OnTextBoxTextChanged;
			}
			if (_dropDownToggle != null)
			{
				_dropDownToggle.Checked += OnDropDownToggleChecked;
				_dropDownToggle.Unchecked += OnDropDownToggleUnchecked;
			}
			if (_popup != null)
			{
				_popup.Opened += OnPopupOpened;
				_popup.Closed += OnPopupClosed;
			}
			if (_palette != null)
			{
				_palette.ItemsSource = Palette;
				_palette.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnPaletteClick));
			}
			WireSlider(_redSlider);
			WireSlider(_greenSlider);
			WireSlider(_blueSlider);
			WireSlider(_alphaSlider);

			RefreshDisplay();
		}

		private void UnwireTemplateParts()
		{
			if (_textBox != null)
			{
				_textBox.LostFocus -= OnTextBoxLostFocus;
				_textBox.PreviewKeyDown -= OnTextBoxKeyDown;
				_textBox.TextChanged -= OnTextBoxTextChanged;
			}
			if (_dropDownToggle != null)
			{
				_dropDownToggle.Checked -= OnDropDownToggleChecked;
				_dropDownToggle.Unchecked -= OnDropDownToggleUnchecked;
			}
			if (_popup != null)
			{
				_popup.Opened -= OnPopupOpened;
				_popup.Closed -= OnPopupClosed;
			}
			if (_redSlider != null) _redSlider.ValueChanged -= OnSliderValueChanged;
			if (_greenSlider != null) _greenSlider.ValueChanged -= OnSliderValueChanged;
			if (_blueSlider != null) _blueSlider.ValueChanged -= OnSliderValueChanged;
			if (_alphaSlider != null) _alphaSlider.ValueChanged -= OnSliderValueChanged;
		}

		private void WireSlider(Slider slider)
		{
			if (slider != null) slider.ValueChanged += OnSliderValueChanged;
		}

		/// <summary>Pushes the current Text into the swatch, sliders, value labels and (when idle) the text box.</summary>
		private void RefreshDisplay()
		{
			_updating = true;
			try
			{
				if (HexColor.TryGetBytes(Text, out byte r, out byte g, out byte b, out byte a))
				{
					if (_swatch != null) _swatch.Background = new SolidColorBrush(Color.FromArgb(a, r, g, b));
					if (_redSlider != null) _redSlider.Value = r;
					if (_greenSlider != null) _greenSlider.Value = g;
					if (_blueSlider != null) _blueSlider.Value = b;
					if (_alphaSlider != null) _alphaSlider.Value = a;
				}
				if (_redValue != null) _redValue.Text = ((int?)_redSlider?.Value ?? 0).ToString();
				if (_greenValue != null) _greenValue.Text = ((int?)_greenSlider?.Value ?? 0).ToString();
				if (_blueValue != null) _blueValue.Text = ((int?)_blueSlider?.Value ?? 0).ToString();
				if (_alphaValue != null) _alphaValue.Text = ((int?)_alphaSlider?.Value ?? 0).ToString();
				if (_textBox != null && (_forceSyncTextBox || !_textBox.IsKeyboardFocusWithin)) _textBox.Text = Text;
			}
			finally
			{
				_updating = false;
			}
		}

		private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
			// Live preview only; the value is committed on focus loss / Enter.
			if (_updating || _swatch == null) return;
			if (HexColor.TryGetBytes(_textBox.Text, out byte r, out byte g, out byte b, out byte a))
				_swatch.Background = new SolidColorBrush(Color.FromArgb(a, r, g, b));
		}

		private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			CommitTextBox();
		}

		private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				CommitTextBox();
				e.Handled = true;
			}
			else if (e.Key == Key.Escape)
			{
				_updating = true;
				try { _textBox.Text = Text; }
				finally { _updating = false; }
				e.Handled = true;
			}
		}

		// Normalizes the typed text if possible, otherwise reverts to the last valid value.
		private void CommitTextBox()
		{
			if (_textBox == null) return;
			string committed = HexColor.TryNormalize(_textBox.Text, out string normalized) ? normalized : Text;
			SetCurrentValue(TextProperty, committed);
			_updating = true;
			try { _textBox.Text = committed; }
			finally { _updating = false; }
		}

		private void OnDropDownToggleChecked(object sender, RoutedEventArgs e)
		{
			if (_popup != null) _popup.IsOpen = true;
		}

		private void OnDropDownToggleUnchecked(object sender, RoutedEventArgs e)
		{
			if (_popup != null && _popup.IsOpen) _popup.IsOpen = false;
		}

		private void OnPopupOpened(object sender, EventArgs e)
		{
			RefreshDisplay();
		}

		private void OnPopupClosed(object sender, EventArgs e)
		{
			CommitTextBox();
			if (_dropDownToggle != null) _dropDownToggle.IsChecked = false;
		}

		private void OnPaletteClick(object sender, RoutedEventArgs e)
		{
			if (e.OriginalSource is FrameworkElement source && source.DataContext is string hex)
			{
				ApplyExternalValue(HexColor.Normalize(hex));
				if (_popup != null) _popup.IsOpen = false;
			}
		}

		private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (_updating) return;
			// Apply any typed-but-uncommitted hex first so slider tweaks build on it.
			CommitTextBox();
			// No alpha slider (Gauntlet ignores text opacity; fades use Brush.GlobalAlphaFactor) - keep the alpha already in Text.
			HexColor.TryGetBytes(Text, out _, out _, out _, out byte a);
			ApplyExternalValue(HexColor.FromBytes(
				(byte)(_redSlider?.Value ?? 255),
				(byte)(_greenSlider?.Value ?? 255),
				(byte)(_blueSlider?.Value ?? 255),
				a));
		}

		/// <summary>Applies a value chosen from the popup, forcing the (still focused) text box to sync.</summary>
		private void ApplyExternalValue(string value)
		{
			_forceSyncTextBox = true;
			try { SetCurrentValue(TextProperty, value); }
			finally { _forceSyncTextBox = false; }
		}
	}

	/// <summary>Converts a hex color string into a frozen WPF brush (for palette swatch templates).</summary>
	public class HexToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (HexColor.TryGetBytes(value as string, out byte r, out byte g, out byte b, out byte a))
			{
				var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
				brush.Freeze();
				return brush;
			}
			return Brushes.Transparent;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Binding.DoNothing;
		}
	}
}
