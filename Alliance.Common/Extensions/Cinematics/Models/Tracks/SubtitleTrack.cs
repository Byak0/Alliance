using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
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
		[ConfigProperty(label: "Text", tooltip: "Use {0}, {1}... where dynamic values should appear, then define them in order via 'Text arguments'. Line breaks are supported.")]
		public LocalizedString Text = new LocalizedString("");

		[ConfigProperty(label: "Text arguments", tooltip: "Values filling the {0}, {1}... placeholders of the text, in order.")]
		[SyncToClient]
		public List<ValueSource<string>> TextArgs = new List<ValueSource<string>>();

		[ConfigProperty(label: "Scrolling", tooltip: "Credits-style: the text scrolls from the bottom of the screen to the top over its duration.")]
		public bool Scroll;

		[ConfigProperty(label: "Duration (s)", minValue: 0.1f, maxValue: 60)]
		public float Duration = 3f;

		[ConfigProperty(label: "Fade (s)", tooltip: "Fade in/out duration. 0 = instant.", minValue: 0, maxValue: 5)]
		public float FadeSec = 0.5f;

		[ConfigProperty(label: "Font size", minValue: 8, maxValue: 100)]
		public int FontSize = 28;

		[ConfigProperty(label: "Glow radius", minValue: 0, maxValue: 1)]
		public float GlowRadius = 0.1f;

		[ConfigProperty(label: "Blur", minValue: 0, maxValue: 1)]
		public float Blur = 0.1f;

		[ConfigProperty(label: "Shadow offset", minValue: 0, maxValue: 1)]
		public float ShadowOffset = 0.2f;

		[ConfigProperty(label: "Outline amount", minValue: 0, maxValue: 1)]
		public float OutlineAmount = 0.1f;

		[ConfigProperty(label: "Color", tooltip: "Hex color: #RRGGBBAA", dataType: AllianceData.DataTypes.Color)]
		public string FontColor = "#FFFFFFFF";

		[ConfigProperty(label: "Font", tooltip: "Native font name. Custom fonts can be added under GUI/Fonts.", dataType: AllianceData.DataTypes.Font)]
		public string Font = "Galahad";

		[ConfigProperty(label: "Horizontal")]
		public SubtitleHPosition HPosition = SubtitleHPosition.Center;

		[ConfigProperty(label: "Vertical")]
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
