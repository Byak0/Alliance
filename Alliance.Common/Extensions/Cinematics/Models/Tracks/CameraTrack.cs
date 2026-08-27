using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using TaleWorlds.Engine;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	public enum LookAtMode { None, MainAgent, Entity, Position }

	[Serializable]
	public class CameraKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Camera frame", tooltip: "World position + rotation of the camera. Use \"Capture View\" in the editor to set it from the current in-scene camera.", category: "Transform")]
		public FrameValue Frame = new FrameValue();

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

		[ConfigProperty(label: "Look at", tooltip: "Override camera rotation to aim at a target. Controls the segment leading up to this keyframe.", category: "Target")]
		public LookAtMode LookAt;

		[ConfigProperty(label: "Target entity", tooltip: "Entity to aim at.", category: "Target")]
		public ValueSource<WeakGameEntity> LookAtEntity = new SceneEntityLiteralValue();

		[ConfigProperty(label: "Target position", tooltip: "World position to aim at.", category: "Target")]
		public FrameValue LookAtPosition = new FrameValue();

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
