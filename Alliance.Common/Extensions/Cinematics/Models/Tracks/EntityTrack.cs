using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using TaleWorlds.Engine;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// A staged scene entity action: show/hide, instant reposition (position + facing),
	/// a travelling MoveTo (position and rotation interpolated over MoveDuration), or a
	/// particle effect command. Movement is client-local kinematic - derived from the shared
	/// cinematic clock, so it syncs without traffic and late joiners see the correct pose.
	/// </summary>
	[Serializable]
	public class EntityActionKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Action", tooltip: "SetVisible: show/hide. Teleport: move instantly. MoveTo: travel to the destination frame (rotation included). Fx: particle effect command.")]
		public EntityActionKind Kind = EntityActionKind.SetVisible;

		[ConfigProperty(label: "Entity", tooltip: "Entity to act on: a scene entity (literal, picked visually) or a variable holding one.")]
		[SyncToClient]
		public ValueSource<WeakGameEntity> Entity = new SceneEntityLiteralValue();

		[ConfigProperty(label: "Visible", dependency: "?Kind=SetVisible")]
		public bool Visible = true;

		[ConfigProperty(label: "Destination", tooltip: "Where to move/teleport: a position with facing, or the frame of a marker/variable entity (use a marker's rotation for gates).", dependency: "?Kind!=SetVisible&?Kind!=Fx")]
		public CinematicTarget Destination = new CinematicTarget(CinematicTargetType.Position);

		[ConfigProperty(label: "Duration (s)", tooltip: "Travel time of the MoveTo. Movement is derived from the cinematic clock, so it stays in sync.", minValue: 0.1f, maxValue: 3600, dependency: "?Kind=MoveTo")]
		public float MoveDuration = 1f;

		[ConfigProperty(label: "Easing", tooltip: "Linear = constant speed. SmoothStep = ease-in/out (natural for gates and camera-adjacent moves).", dependency: "?Kind=MoveTo")]
		public EntityMoveEasing Easing = EntityMoveEasing.SmoothStep;

		[ConfigProperty(label: "Effect", tooltip: "Burst: one-shot explosion/muzzle flash. Pause/Resume: continuous effects (smoke, fire).", dependency: "?Kind=Fx")]
		public EntityFxMode Fx = EntityFxMode.Burst;

		[ConfigProperty(label: "Include children", tooltip: "Apply the effect to child entities too (e.g. every cannon of a battery).", dependency: "?Kind=Fx")]
		public bool FxChildren = true;

		public EntityActionKeyframe() { }

		public EntityActionKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class EntityTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<EntityActionKeyframe> Keyframes = new List<EntityActionKeyframe>();

		public EntityTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}
}
