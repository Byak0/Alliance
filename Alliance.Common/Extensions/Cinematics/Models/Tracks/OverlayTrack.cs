using Alliance.Common.Core.Configuration.Models;
using System;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	[Serializable]
	public class OverlayKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Letterbox", tooltip: "Height of the black bars as a fraction of screen height (0 = none, 1 = fully covered).", minValue: 0, maxValue: 1, category: "Bars")]
		public float Letterbox;

		[ConfigProperty(label: "Black", tooltip: "Black fade amount (0 = fully visible, 1 = fully black).", minValue: 0, maxValue: 1, category: "Fade")]
		public float FadeAlpha;

		public OverlayKeyframe() { }

		public OverlayKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class OverlayTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<OverlayKeyframe> Keyframes = new List<OverlayKeyframe>();

		public OverlayTrack() { }

		public override System.Collections.IList GetKeyframes() => Keyframes;
	}
}
