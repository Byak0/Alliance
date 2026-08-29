using Alliance.Common.Core.Configuration.Models;
using System;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// Plays an animation (and/or facial animation) on an agent resolved by role when crossed.
	/// Reuses the same action/facial-animation vocabulary as CinematicCamera.
	/// </summary>
	[Serializable]
	public class AgentAnimationKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Target role", tooltip: "Role name of the agent to animate (e.g. \"Boss\", \"MainAgent\", \"Viewer\").", category: "Target")]
		public string TargetRole = "MainAgent";

		[ConfigProperty(label: "Action", tooltip: "Native action name (e.g. \"act_cheer\"). Leave empty to keep the current action.", category: "Animation")]
		public string ActionName = "";

		[ConfigProperty(label: "Facial animation", tooltip: "Native facial animation name. Leave empty to skip.", category: "Animation")]
		public string FacialAnimation = "";

		[ConfigProperty(label: "Loop", tooltip: "Loop the body action.", category: "Animation")]
		public bool Loop = true;

		public AgentAnimationKeyframe() { }

		public AgentAnimationKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class AgentAnimationTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<AgentAnimationKeyframe> Keyframes = new List<AgentAnimationKeyframe>();

		public AgentAnimationTrack() { }

		public override System.Collections.IList GetKeyframes() => Keyframes;
	}
}
