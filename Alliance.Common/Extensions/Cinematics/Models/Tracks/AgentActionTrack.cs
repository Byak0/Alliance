using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// Stages the subject defined by the track's <see cref="Target"/> (existing agents resolved from
	/// variables, or staged extras spawned as FakeAgents) over the cinematic: teleport, scripted move,
	/// body/facial animations, visibility. True-agent commands execute server-side and replicate
	/// through native agent sync; staged extras are per-machine and run deterministically from the
	/// shared cinematic clock. One lane per track.
	/// </summary>
	[Serializable]
	public class AgentActionTrack : CinematicTrack
	{
		[ConfigProperty(label: "Agent target", tooltip: "The subject this lane stages. Click to edit: true agents from variables, or a staged extras formation (per-client fake agents).")]
		public CinematicAgent Target = new CinematicAgent();

		[ConfigProperty(label: "Keyframes")]
		public List<AgentActionKeyframe> Keyframes = new List<AgentActionKeyframe>();

		public AgentActionTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}

	/// <summary>
	/// A typed command applied to the track's agent when its time is reached. Teleport and MoveTo
	/// destinations are positions/faces; MoveTo uses the native scripted movement (real locomotion,
	/// avoids obstacles) so its arrival time is approximate - author follow-up keyframes with margin.
	/// </summary>
	[Serializable]
	public class AgentActionKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Action", tooltip: "Teleport: instant reposition. MoveTo: walk/run to a destination. PlayAnimation: body action. PlayFacial: facial animation. SetVisible: hide/show the agent (local per receiver).")]
		public AgentActionKind Kind = AgentActionKind.PlayAnimation;

		[ConfigProperty(label: "Destination", tooltip: "Where to teleport/move: a position with facing, the frame of a marker/variable entity, or an agent's position. MoveTo arrival time is approximate.", dependency: "?Kind!=PlayAnimation&?Kind!=PlayFacial&?Kind!=SetVisible")]
		public CinematicTarget Destination = new CinematicTarget(CinematicTargetType.Position);

		[ConfigProperty(label: "Keep formation offset", tooltip: "Each agent keeps its spot relative to the group: true agents keep their current arrangement (centroid + facing, rotated to face the destination), fake agents keep their slot in the authored formation. Unchecked: every agent converges on the exact destination point.", dependency: "?Kind!=PlayAnimation&?Kind!=PlayFacial&?Kind!=SetVisible")]
		public bool KeepFormationOffset = true;

		[ConfigProperty(label: "Speed", tooltip: "Walk or run to the destination. Custom overrides the locomotion speed (m/s) through the native speed limit.", dependency: "?Kind=MoveTo")]
		public AgentMoveSpeed Speed = AgentMoveSpeed.Walk;

		[ConfigProperty(label: "Speed (m/s)", tooltip: "Custom locomotion speed in meters per second, applied while moving (true agents via the native speed limit; staged fakes kinematically). Typical values: walk ~1.6, run ~4.", minValue: 0.1f, maxValue: 15, dependency: "?Kind=MoveTo&?Speed=Custom")]
		public float CustomSpeed = 1.6f;

		[ConfigProperty(label: "Arrival facing (deg)", tooltip: "Final facing in degrees. Negative = face the travel direction.", minValue: -180, maxValue: 180, dependency: "?Kind=MoveTo")]
		public float ArrivalFacingDeg = -1f;

		[ConfigProperty(label: "Move clip", tooltip: "Animation clip played on the RIDER while moving (fake agents only; true agents play the native walk/run locomotion). Leave empty to slide.", dependency: "?Kind=MoveTo")]
		public string MoveAnimation = "";

		[ConfigProperty(label: "Mount move clip", tooltip: "Animation clip played on the MOUNT while moving (fake agents only). Leave empty to auto-pick the horse walk/trot from the pace.", dependency: "?Kind=MoveTo")]
		public string MountMoveAnimation = "";

		[ConfigProperty(label: "Action", tooltip: "Native body action played by TRUE agents (e.g. act_cheer). Pick precise shots with the timeline: the action starts when this keyframe is crossed.", dependency: "?Kind=PlayAnimation")]
		public string ActionName = "";

		[ConfigProperty(label: "Clip", tooltip: "Raw animation clip played by FAKE agents (staged extras, preview stand-ins). Independent from the Action field above. Clips loop when authored as looping.", dependency: "?Kind=PlayAnimation")]
		public string ClipName = "";

		[ConfigProperty(label: "Mount action", tooltip: "Native body action played on the mount of a TRUE agent. Leave empty to let the mount keep its idle.", dependency: "?Kind=PlayAnimation")]
		public string MountActionName = "";

		[ConfigProperty(label: "Mount clip", tooltip: "Raw animation clip played on the mount of a FAKE agent. Leave empty to let the mount keep its idle.", dependency: "?Kind=PlayAnimation")]
		public string MountClipName = "";

		[ConfigProperty(label: "Channel", tooltip: "0 = replaces locomotion. 1+ = overlays it (gesture while walking).", minValue: 0, maxValue: 3, dependency: "?Kind=PlayAnimation")]
		public int Channel;

		[ConfigProperty(label: "Loop", tooltip: "Keep the animation looping until another keyframe changes it. True agents: native cyclic flag. Fake agents: the clip is re-fired on its native duration (walk cycles, idles...); clips authored as looping play continuously regardless.", dependency: "?Kind=PlayAnimation")]
		public bool Loop;

		[ConfigProperty(label: "Speed", tooltip: "Playback speed multiplier of the action.", minValue: 0.1f, maxValue: 4, dependency: "?Kind=PlayAnimation")]
		public float ActionSpeed = 1f;

		[ConfigProperty(label: "Facial animation", tooltip: "Native facial animation name.", dependency: "?Kind=PlayFacial")]
		public string FacialAnimName = "";

		[ConfigProperty(label: "Loop", dependency: "?Kind=PlayFacial")]
		public bool FacialLoop = true;

		[ConfigProperty(label: "Visible", dependency: "?Kind=SetVisible")]
		public bool Visible = false;

		[ConfigProperty(label: "Include mount", tooltip: "Hide/show the agent's mount together with it.", dependency: "?Kind=SetVisible")]
		public bool IncludeMount = true;

		public AgentActionKeyframe() { }

		public AgentActionKeyframe(float time) : base(time) { }
	}
}
