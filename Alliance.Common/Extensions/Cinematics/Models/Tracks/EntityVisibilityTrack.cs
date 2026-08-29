using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// Shows or hides a scene entity when crossed.
	/// </summary>
	[Serializable]
	public class EntityVisibilityKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Entity", tooltip: "The entity to toggle.", category: "Target")]
		public GameEntityRef Entity = new GameEntityRef();

		[ConfigProperty(label: "Visible", category: "Target")]
		public bool Visible = true;

		public EntityVisibilityKeyframe() { }

		public EntityVisibilityKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class EntityVisibilityTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<EntityVisibilityKeyframe> Keyframes = new List<EntityVisibilityKeyframe>();

		public EntityVisibilityTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}
}
