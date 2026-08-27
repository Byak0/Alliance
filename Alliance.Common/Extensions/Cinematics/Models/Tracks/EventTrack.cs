using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Actions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// Fires a list of scenario actions when the playhead crosses this keyframe's time.
	/// Because it reuses <c>ActionBase</c> directly,
	/// anything possible in a scenario conditional action is possible at a cinematic timestamp.
	/// </summary>
	[Serializable]
	public class EventKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Actions", tooltip: "Fired once when the playhead reaches this keyframe (server-authoritative).")]
		public List<ActionBase> Actions = new List<ActionBase>();

		public EventKeyframe() { }

		public EventKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class EventTrack : CinematicTrack
	{
		[ConfigProperty(label: "Events", tooltip: "Timed triggers; each fires its actions when crossed.")]
		public List<EventKeyframe> Keyframes = new List<EventKeyframe>();

		public EventTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}
}
