namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>
	/// How a value moves between two keyframes.
	/// </summary>
	public enum Interpolation
	{
		CatmullRom,
		Linear,
		Constant
	}

	/// <summary>
	/// What happens to agents while a cinematic is playing. Free/Lock affect the local player;
	/// the Hide modes also freeze the local player and hide the matching agents (and their mounts)
	/// locally on every receiver - remote players keep control of their own agents.
	/// </summary>
	public enum AgentBehaviorMode
	{
		Free,
		Lock,
		HidePlayers,
		HideAll
	}

	/// <summary>
	/// Who is made invulnerable (server-side, mission-wide) for the duration of a cinematic.
	/// </summary>
	public enum InvulnerabilityMode
	{
		None,
		Players,
		Bots,
		All
	}

	/// <summary>
	/// Who receives (and plays locally) a cinematic.
	/// </summary>
	public enum AudienceScope
	{
		All,
		Team,
		Players
	}

	public enum CameraCutMode
	{
		Cut,
		Blend
	}

	/// <summary>
	/// What an EntityTrack keyframe does to its entity when its time is reached.
	/// Teleport and MoveTo destinations are full frames (position + facing), so gates rotate
	/// open and ships sail. MoveTo is a section lasting MoveDuration.
	/// </summary>
	public enum EntityActionKind
	{
		SetVisible,
		Teleport,
		MoveTo,
		Fx
	}

	/// <summary>
	/// Particle effect command applied to an entity: Burst fires a one-shot explosion/muzzle flash,
	/// Pause/Resume control continuous effects (smoke columns, fires).
	/// </summary>
	public enum EntityFxMode
	{
		Burst,
		Pause,
		Resume
	}

	/// <summary>
	/// Easing of an entity MoveTo interpolation.
	/// </summary>
	public enum EntityMoveEasing
	{
		Linear,
		SmoothStep
	}

	/// <summary>
	/// What an AgentActionTrack keyframe makes its agent do. Commands execute server-side
	/// (the track target is a scenario variable); locomotion and body animations replicate
	/// through native agent sync.
	/// </summary>
	public enum AgentActionKind
	{
		Teleport,
		MoveTo,
		PlayAnimation,
		PlayFacial,
		SetVisible
	}

	/// <summary>
	/// Locomotion pace of an agent MoveTo. Walk/Run map to the native scripted-move DoNotRun flag;
	/// Custom overrides the native speed limit with the keyframe's <c>CustomSpeed</c> (m/s).
	/// </summary>
	public enum AgentMoveSpeed
	{
		Walk,
		Run,
		Custom
	}
}
