using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Bridge between the shared <see cref="CinematicView"/> (Common) and editor-only puppet staging.
	/// The modding-kit module registers <see cref="PreviewAgentActionHandler"/> when a preview starts;
	/// agent action keyframes fired during the preview are then executed against preview puppets
	/// (real agents do not exist in the editor scene, and Common cannot reference puppet machinery).
	/// </summary>
	public static class CinematicPreviewBridge
	{
		public static Action<AgentActionTrack, AgentActionKeyframe> PreviewAgentActionHandler { get; set; }
	}
}
