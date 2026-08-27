using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	public enum SubtitleHPosition { Left, Center, Right }
	public enum SubtitleVPosition { Top, Center, Bottom }

	[Serializable]
	public class SubtitleKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Text", category: "Subtitle")]
		public LocalizedString Text = new LocalizedString("");

		[ConfigProperty(label: "Duration (s)", minValue: 0.1f, maxValue: 60, category: "Subtitle")]
		public float Duration = 3f;

		[ConfigProperty(label: "Fade (s)", tooltip: "Fade in/out duration. 0 = instant.", minValue: 0, maxValue: 5, category: "Subtitle")]
		public float FadeSec = 0.5f;

		[ConfigProperty(label: "Font size", minValue: 8, maxValue: 100, category: "Style")]
		public int FontSize = 28;

		[ConfigProperty(label: "Color", tooltip: "Hex color: #RRGGBBAA", dataType: AllianceData.DataTypes.Color, category: "Style")]
		public string FontColor = "#FFFFFFFF";

		[ConfigProperty(label: "Font", tooltip: "Native font name. Custom fonts can be added under GUI/Fonts.", dataType: AllianceData.DataTypes.Font, category: "Style")]
		public string Font = "Galahad";

		[ConfigProperty(label: "Horizontal", category: "Style")]
		public SubtitleHPosition HPosition = SubtitleHPosition.Center;

		[ConfigProperty(label: "Vertical", category: "Style")]
		public SubtitleVPosition VPosition = SubtitleVPosition.Bottom;

		public SubtitleKeyframe() { }
		public SubtitleKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class SubtitleTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<SubtitleKeyframe> Keyframes = new List<SubtitleKeyframe>();

		public SubtitleTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}
}
