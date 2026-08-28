#if !SERVER
using Alliance.Common.Extensions.Cinematics.Models;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace Alliance.Common.Extensions.Cinematics
{
	public class CinematicOverlayVM : ViewModel
	{
		private float _fadeAlpha;
		private float _letterboxHeight;
		private bool _isSkippable;

		[DataSourceProperty]
		public float FadeAlpha
		{
			get => _fadeAlpha;
			set { if (value != _fadeAlpha) { _fadeAlpha = value; OnPropertyChangedWithValue(value); } }
		}

		[DataSourceProperty]
		public float LetterboxHeight
		{
			get => _letterboxHeight;
			set { if (value != _letterboxHeight) { _letterboxHeight = value; OnPropertyChangedWithValue(value); } }
		}

		/// <summary>Shows the "Press Space to skip" hint. Skipping is local-only.</summary>
		[DataSourceProperty]
		public bool IsSkippable
		{
			get => _isSkippable;
			set { if (value != _isSkippable) { _isSkippable = value; OnPropertyChangedWithValue(value); } }
		}

		public MBBindingList<SubtitleItemVM> Subtitles { get; } = new MBBindingList<SubtitleItemVM>();

		private readonly Dictionary<object, SubtitleItemVM> _subtitleMap = new Dictionary<object, SubtitleItemVM>();
		private readonly HashSet<object> _seenSources = new HashSet<object>();
		private bool _hadSubtitles;

		public void UpdateSubtitles(List<SubtitleState> subtitles)
		{
			bool any = subtitles != null && subtitles.Count > 0;
			if (!any && !_hadSubtitles) return;
			_hadSubtitles = any;

			if (!any)
			{
				Subtitles.Clear();
				_subtitleMap.Clear();
				return;
			}

			_seenSources.Clear();
			foreach (SubtitleState state in subtitles)
			{
				if (state.Source == null || !_seenSources.Add(state.Source)) continue;
				if (_subtitleMap.TryGetValue(state.Source, out SubtitleItemVM vm))
				{
					ApplyState(vm, state);
				}
				else
				{
					vm = new SubtitleItemVM { Source = state.Source };
					ApplyState(vm, state);
					_subtitleMap[state.Source] = vm;
					Subtitles.Add(vm);
				}
			}

			List<object> removed = null;
			foreach (KeyValuePair<object, SubtitleItemVM> kv in _subtitleMap)
				if (!_seenSources.Contains(kv.Key)) (removed ??= new List<object>()).Add(kv.Key);
			if (removed != null)
			{
				foreach (object key in removed)
				{
					Subtitles.Remove(_subtitleMap[key]);
					_subtitleMap.Remove(key);
				}
			}
		}

		private static void ApplyState(SubtitleItemVM vm, in SubtitleState state)
		{
			vm.Text = state.Text;
			vm.Alpha = state.Alpha;
			vm.FontSize = state.FontSize;
			vm.FontColor = state.FontColor;
			vm.Font = state.Font;
			vm.HAlign = state.HAlign;
			vm.VAlign = state.VAlign;
		}
	}

	public class SubtitleItemVM : ViewModel
	{
		private string _text = "";
		private float _alpha;
		private int _fontSize = 28;
		private string _fontColor = "#FFFFFFFF";
		private string _font = "Galahad";
		private HorizontalAlignment _hAlign = HorizontalAlignment.Center;
		private VerticalAlignment _vAlign = VerticalAlignment.Bottom;

		/// <summary>The keyframe this item renders - stable identity for recycling across frames.</summary>
		internal object Source { get; set; }

		[DataSourceProperty]
		public string Text { get => _text; set { _text = value; OnPropertyChangedWithValue(value); } }

		[DataSourceProperty]
		public float Alpha { get => _alpha; set { _alpha = value; OnPropertyChangedWithValue(value); } }

		[DataSourceProperty]
		public int FontSize { get => _fontSize; set { _fontSize = value; OnPropertyChangedWithValue(value); } }

		[DataSourceProperty]
		public string FontColor
		{
			get => _fontColor;
			set
			{
				// Gauntlet's Color.ConvertStringToColor throws (and crashes) on anything but #RRGGBBAA
				string normalized = HexColor.Normalize(value);
				if (normalized != _fontColor)
				{
					_fontColor = normalized;
					OnPropertyChangedWithValue(normalized);
				}
			}
		}

		[DataSourceProperty]
		public string Font
		{
			get => _font;
			set
			{
				if (value != _font)
				{
					_font = value;
					OnPropertyChangedWithValue(value);
					OnPropertyChanged(nameof(FontObject));
				}
			}
		}

		/// <summary>Resolved Gauntlet font for <see cref="Font"/>, bound by the prefab as Brush.Font.</summary>
		[DataSourceProperty]
		public Font FontObject => UIResourceManager.FontFactory?.GetMappedFontForLocalization(_font);

		[DataSourceProperty]
		public HorizontalAlignment HAlign { get => _hAlign; set { _hAlign = value; OnPropertyChanged(); } }

		[DataSourceProperty]
		public VerticalAlignment VAlign { get => _vAlign; set { _vAlign = value; OnPropertyChanged(); } }
	}
}
#endif