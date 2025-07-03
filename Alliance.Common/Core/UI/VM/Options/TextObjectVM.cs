﻿using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Alliance.Common.Core.UI.VM.Options
{
	/// <summary>
	/// Simple hidable VM for storing TextObject.
	/// </summary>
	public class TextObjectVM : ViewModel
	{
		private TextObject _textObject;
		private string _text;
		private bool _isVisible;

		public TextObject TextObject
		{
			get
			{
				return _textObject;
			}
			set
			{
				_textObject = value;
				Text = _textObject.ToString();
			}
		}

		[DataSourceProperty]
		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				if (_text != value)
				{
					_text = value;
					OnPropertyChanged(nameof(Text));
				}
			}
		}

		[DataSourceProperty]
		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			set
			{
				if (_isVisible != value)
				{
					_isVisible = value;
					OnPropertyChanged(nameof(IsVisible));
				}
			}
		}

		public TextObjectVM(TextObject text, bool isVisible = true)
		{
			TextObject = text;
			IsVisible = isVisible;
		}

		public override void RefreshValues()
		{
			base.RefreshValues();
		}
	}
}
