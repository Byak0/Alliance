using Alliance.Common.Core.Configuration.Models;
using System;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// Base for all cinematic keyframes. Concrete tracks store their own typed keyframe subclasses
	/// (e.g. <see cref="Tracks.CameraKeyframe"/>) in concrete <c>List&lt;T&gt;</c> fields, so each value
	/// type is serialized directly.
	/// </summary>
	[Serializable]
	public abstract class CinematicKeyframe
	{
		[ConfigProperty(label: "Time", tooltip: "When this keyframe applies, in seconds from the start of the cinematic.", minValue: 0, maxValue: 3600)]
		public float Time;

		[ConfigProperty(label: "Interpolation", tooltip: "How the value moves from the previous keyframe to this one.", category: "Curve")]
		public Interpolation Interpolation = Interpolation.CatmullRom;

		[ConfigProperty(label: "Tension", tooltip: "CatmullRom only. 0 = smooth, 1 = straight, negative = curvier.", minValue: -1, maxValue: 1, category: "Curve")]
		public float Tension;

		public CinematicKeyframe() { }

		public CinematicKeyframe(float time) => Time = time;
	}
}
