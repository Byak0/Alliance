using Alliance.Common.Core.Configuration.Models;
using System;
using System.Collections;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// Base for all cinematic tracks. A <see cref="Cinematic"/> holds a polymorphic
	/// <c>List&lt;CinematicTrack&gt;</c>; concrete subclasses declare their own typed keyframe list.
	/// Derived tracks are auto-discovered by <c>ScenarioSerializer</c>'s derived-type scan.
	/// </summary>
	[Serializable]
	public abstract class CinematicTrack
	{
		[ConfigProperty(label: "Enabled")]
		public bool Enabled = true;

		[ConfigProperty(label: "Name", tooltip: "Optional display name shown in the timeline.")]
		public string Name = "";

		[ConfigProperty(label: "Muted", tooltip: "When true, the player ignores this track during playback/preview.")]
		public bool Muted;

		public CinematicTrack() { }

		/// <summary>Non-generic access to the track's keyframe list (used by duration/validator code that
		/// walks tracks generically). Returns the concrete keyframe list declared by the subclass.</summary>
		public abstract IList GetKeyframes();
	}
}
