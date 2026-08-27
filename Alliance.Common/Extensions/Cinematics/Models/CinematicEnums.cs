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
	/// What happens to the player's main agent while a cinematic is playing.
	/// </summary>
	public enum AgentBehaviorMode
	{
		Hide,
		Lock,
		Free
	}

	/// <summary>
	/// Who receives (and plays locally) a cinematic.
	/// </summary>
	public enum AudienceScope
	{
		All,
		Team,
		Players,
		RelativeToViewer
	}

	public enum CameraCutMode
	{
		Cut,
		Blend
	}
}
