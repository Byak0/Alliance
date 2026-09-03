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
}
