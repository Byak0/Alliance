using Alliance.Common.Core.Configuration.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// Plays a sound event (voice line, SFX, music sting) when crossed.
	/// The audio listener follows the cinematic camera during playback.
	/// </summary>
	[Serializable]
	public class AudioKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Sound event", tooltip: "Native sound event path (e.g. \"event:/vo/narrator_intro\").", category: "Audio")]
		public string SoundEvent = "";

		[ConfigProperty(label: "Volume", minValue: 0, maxValue: 2, category: "Audio")]
		public float Volume = 1f;

		[ConfigProperty(label: "Loop", category: "Audio")]
		public bool Loop;

		public AudioKeyframe() { }

		public AudioKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class AudioTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<AudioKeyframe> Keyframes = new List<AudioKeyframe>();

		public AudioTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}
}
