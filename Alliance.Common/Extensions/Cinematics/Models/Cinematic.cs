using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// A data-driven cinematic: a timeline of tracks/keyframes that drive the camera, audio, visibility, agents and game events.
	/// Serialized inside a <see cref="Scenario"/> or a <c>PlayCinematicAction</c> within an <c>AL_TriggerAction</c> entity.
	/// </summary>
	[Serializable]
	public class Cinematic
	{
		[ConfigProperty(false)]
		public string Id;

		[ConfigProperty(label: "Name")]
		public string Name = "New cinematic";

		[ConfigProperty(label: "Duration (s)", tooltip: "Total length. Leave 0 to derive from the last keyframe time.", minValue: 0, maxValue: 3600)]
		public float DurationSec;

		[ConfigProperty(label: "Loop")]
		public bool Loop;

		[ConfigProperty(label: "Skippable", tooltip: "Show a skip prompt to players.")]
		public bool IsSkippable = true;

		[ConfigProperty(label: "Fade in (s)", tooltip: "Fade from black at the start of the cinematic. Only used when no Overlay track is present.", minValue: 0, maxValue: 10, category: "Transitions")]
		public float FadeInSec;

		[ConfigProperty(label: "Fade out (s)", tooltip: "Fade to black at the end of the cinematic. Only used when no Overlay track is present.", minValue: 0, maxValue: 10, category: "Transitions")]
		public float FadeOutSec;

		[ConfigProperty(label: "Main agent", tooltip: "What happens to the player's main agent during playback.", category: "Playback")]
		public AgentBehaviorMode AgentBehavior = AgentBehaviorMode.Lock;

		[ConfigProperty(label: "Audience", tooltip: "Who sees this cinematic.", category: "Playback")]
		public Audience Audience = new Audience();

		[ConfigProperty(label: "Tracks", tooltip: "The timeline tracks (camera, audio, events, ...). Add as many as you need.")]
		public List<CinematicTrack> Tracks = new List<CinematicTrack>();

		public Cinematic()
		{
			Id = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
				.Substring(0, 8)
				.Replace("/", "_").Replace("+", "-");
		}

		/// <summary>Effective duration: <see cref="DurationSec"/> if set, otherwise the latest keyframe end
		/// (keyframe time, plus display duration for hold-style keyframes such as subtitles).</summary>
		public float GetDuration()
		{
			if (DurationSec > 0f) return DurationSec;
			float max = 0f;
			foreach (var track in Tracks ?? Enumerable.Empty<CinematicTrack>())
			{
				if (track == null) continue;
				System.Collections.IList keyframes = track.GetKeyframes();
				if (keyframes == null) continue;
				foreach (var kf in keyframes)
				{
					if (kf is not CinematicKeyframe ckf) continue;
					float end = ckf.Time;
					if (ckf is SubtitleKeyframe subtitle && subtitle.Duration > 0f) end += subtitle.Duration;
					if (end > max) max = end;
				}
			}
			return max;
		}

		/// <summary>Returns the first enabled, non-muted track of the given type, or null.</summary>
		public T FindTrack<T>() where T : CinematicTrack
			=> (Tracks ?? Enumerable.Empty<CinematicTrack>())
				.OfType<T>().FirstOrDefault(t => t != null && t.Enabled && !t.Muted);
	}

	/// <summary>
	/// Lightweight reference to a <see cref="Cinematic"/> stored on the owning <see cref="Scenario"/>.
	/// Resolved at runtime via <see cref="ScenarioManager"/>'s <c>CurrentScenario.Cinematics</c>.
	/// </summary>
	[Serializable]
	public class CinematicRef
	{
		[ConfigProperty(label: "Cinematic Id", tooltip: "The Id of a cinematic defined on this scenario. Leave empty to use an inline cinematic.")]
		public string CinematicId = "";

		public CinematicRef() { }

		public CinematicRef(string cinematicId) => CinematicId = cinematicId;

		public bool IsEmpty => string.IsNullOrEmpty(CinematicId);
	}
}
