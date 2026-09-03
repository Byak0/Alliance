using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// Aiming override for the camera. When the active <see cref="LookAtTrack"/> resolves a target,
	/// the player recomputes the camera rotation to point at it.
	/// </summary>
	[Serializable]
	public class LookAtKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Target", tooltip: "What the camera aims at while this keyframe is active. Target = None disables the override.", category: "Target")]
		public CinematicTarget Target = new CinematicTarget();

		public LookAtKeyframe() { }

		public LookAtKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class LookAtTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<LookAtKeyframe> Keyframes = new List<LookAtKeyframe>();

		public LookAtTrack() { }

		public override System.Collections.IList GetKeyframes() => Keyframes;
	}
}
