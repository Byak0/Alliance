using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Alliance.Editor.GameModes.Story.Widgets
{
	/// <summary>
	/// A control that allows the user to input a numeric value using buttons, text input or mouse wheel.
	/// </summary>
	public class NumericUpDownBox : Control
	{
		private Button _incrementButton;
		private Button _decrementButton;
		private TextBox _textBox;

		static NumericUpDownBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDownBox), new FrameworkPropertyMetadata(typeof(NumericUpDownBox)));
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_incrementButton = GetTemplateChild("PART_IncrementButton") as Button;
			_decrementButton = GetTemplateChild("PART_DecrementButton") as Button;
			_textBox = GetTemplateChild("PART_TextBox") as TextBox;

			if (_incrementButton != null)
			{
				_incrementButton.Click += (s, e) => IncrementValue();
			}

			if (_decrementButton != null)
			{
				_decrementButton.Click += (s, e) => DecrementValue();
			}

			if (_textBox != null)
			{
				_textBox.PreviewTextInput += OnPreviewTextInput;
				_textBox.MouseWheel += OnMouseWheel;
				_textBox.LostFocus += OnLostFocus; // Handle lost focus to apply the value
				DataObject.AddPastingHandler(_textBox, OnPaste);
			}

			// Ensure the initial value is displayed correctly
			OnValueChanged(null, Value);
		}

		public static readonly DependencyProperty ValueProperty =
			DependencyProperty.Register(nameof(Value), typeof(object), typeof(NumericUpDownBox),
			new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

		public object Value
		{
			get => GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = (NumericUpDownBox)d;
			control.OnValueChanged(e.OldValue, e.NewValue);
		}

		private void OnValueChanged(object oldValue, object newValue)
		{
			if (_textBox != null)
			{
				// Explicitly handle int and float cases
				if (newValue is int intValue)
				{
					_textBox.Text = intValue.ToString();
				}
				else if (newValue is float floatValue)
				{
					_textBox.Text = floatValue.ToString("F2"); // Limit decimal places for better display
				}
			}
		}

		private static object CoerceValue(DependencyObject d, object value)
		{
			if (value is int || value is float)
			{
				return value;
			}

			return 0.0;
		}

		private void IncrementValue()
		{
			if (Value is int intValue)
			{
				Value = intValue + 1;
			}
			else if (Value is float floatValue)
			{
				Value = floatValue + 0.1f;
			}
		}

		private void DecrementValue()
		{
			if (Value is int intValue)
			{
				Value = intValue - 1;
			}
			else if (Value is float floatValue)
			{
				Value = floatValue - 0.1f;
			}
		}

		private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = !IsTextNumeric(e.Text, true);
		}

		private static bool IsTextNumeric(string text, bool allowDecimal = true)
		{
			// Allow decimal points if specified
			var cultureSeparator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
			return int.TryParse(text, out _) ||
				   float.TryParse(text, out _) ||
				   allowDecimal && (text == cultureSeparator || text == ".");
		}

		private void OnMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta > 0)
			{
				IncrementValue();
			}
			else if (e.Delta < 0)
			{
				DecrementValue();
			}
			e.Handled = true;
		}

		private void OnPaste(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(string)))
			{
				string text = (string)e.DataObject.GetData(typeof(string));
				if (!IsTextNumeric(text))
				{
					e.CancelCommand();
				}
			}
			else
			{
				e.CancelCommand();
			}
		}

		private void OnLostFocus(object sender, RoutedEventArgs e)
		{
			// When the TextBox loses focus, update the Value based on the current input
			if (_textBox != null && !string.IsNullOrWhiteSpace(_textBox.Text))
			{
				if (Value is int && int.TryParse(_textBox.Text, out var intValue))
				{
					Value = intValue;
				}
				else if (Value is float && float.TryParse(_textBox.Text, out var floatValue))
				{
					Value = floatValue;
				}
			}
		}
	}
}
