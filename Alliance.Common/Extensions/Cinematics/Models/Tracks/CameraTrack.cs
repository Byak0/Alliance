using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	public enum CameraFrameMode { Absolute, Relative }

	/// <summary>Frozen = capture the target frame once when the cinematic starts. Track = follow the target every tick.</summary>
	public enum TargetTrackMode { Frozen, Track }

	[Serializable]
	public class CameraKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Camera frame", tooltip: "World position + rotation of the camera. Use \"Capture View\" in the editor to set it from the current in-scene camera.", category: "Transform", dependency: "?FrameMode=Absolute")]
		public FrameValue Frame = new FrameValue();

		[ConfigProperty(label: "Frame mode", tooltip: "Absolute = world-space frame. Relative = frame resolved from the target below, with the offset applied in the target's local space (enables travels like player camera → point of interest → player's agent).", category: "Transform")]
		public CameraFrameMode FrameMode;

		[ConfigProperty(label: "Frame target", tooltip: "What the camera frame is relative to.", category: "Transform", dependency: "?FrameMode=Relative")]
		public CinematicTarget FrameTarget = new CinematicTarget(CinematicTargetType.ViewerCamera);

		[ConfigProperty(label: "Frame offset", tooltip: "Offset applied in the target's local space when Frame mode = Relative (e.g. 3m behind, 1m above).", category: "Transform", dependency: "?FrameMode=Relative")]
		public FrameValue FrameOffset = new FrameValue();

		[ConfigProperty(label: "Tracking", tooltip: "Frozen = capture the target frame once at the start. Track = follow the target every tick.", category: "Transform", dependency: "?FrameMode=Relative")]
		public TargetTrackMode TrackMode = TargetTrackMode.Track;

		[ConfigProperty(label: "FOV (vertical, degrees)", minValue: 1, maxValue: 179, category: "Lens")]
		public float Fov = 60f;

		[ConfigProperty(label: "Near plane", minValue: 0.001f, maxValue: 10, category: "Lens")]
		public float NearPlane = 0.1f;

		[ConfigProperty(label: "Far plane", minValue: 1, maxValue: 100000, category: "Lens")]
		public float FarPlane = 12500f;

		[ConfigProperty(label: "Roll (radians)", minValue: -3.14159f, maxValue: 3.14159f, category: "Lens")]
		public float Roll;

		[ConfigProperty(label: "Depth of field", tooltip: "Enable photo-mode DoF for this keyframe.", category: "Depth of Field")]
		public bool DoFEnabled;

		[ConfigProperty(label: "Focus distance", minValue: 0, maxValue: 1000, category: "Depth of Field", dependency: "?DoFEnabled")]
		public float FocusDistance = 5f;

		[ConfigProperty(label: "DoF start", minValue: 0, maxValue: 1000, category: "Depth of Field", dependency: "?DoFEnabled")]
		public float DoFStart = 1f;

		[ConfigProperty(label: "DoF end", minValue: 0, maxValue: 10000, category: "Depth of Field", dependency: "?DoFEnabled")]
		public float DoFEnd = 20f;

		[ConfigProperty(label: "Exposure", minValue: 0, maxValue: 8, category: "Depth of Field", dependency: "?DoFEnabled")]
		public float Exposure = 1f;

		public CameraKeyframe() { }

		public CameraKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class CameraTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes", tooltip: "Camera samples over time. Default interpolation is a smooth Catmull-Rom path between positions.")]
		public List<CameraKeyframe> Keyframes = new List<CameraKeyframe>();

		public CameraTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}
}
