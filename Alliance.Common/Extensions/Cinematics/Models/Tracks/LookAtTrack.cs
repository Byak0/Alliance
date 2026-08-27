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
		[ConfigProperty(label: "Target role", tooltip: "Role name resolved at runtime (e.g. \"Boss\", \"MainAgent\", \"Viewer\"). Leave empty to use the explicit position below.", category: "Target")]
		public string TargetRole = "";

		[ConfigProperty(label: "Use explicit position", tooltip: "Bypass role resolution and aim at the position below.", category: "Target")]
		public bool UsePosition;

		[ConfigProperty(label: "Target position", tooltip: "World position to aim at when no role is used (or the role cannot be resolved).", category: "Target", dependency: "?UsePosition")]
		public FrameValue TargetPosition = new FrameValue();

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
