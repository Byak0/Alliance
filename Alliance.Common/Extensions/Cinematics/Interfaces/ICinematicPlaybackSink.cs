using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Interface for classes that applies cinematic effects (CinematicView on client, ServerCinematicSink on server).
	/// Passed to <see cref="CinematicPlayer"/> 
	/// </summary>
	public interface ICinematicPlaybackSink
	{
		bool RequiresVisualSampling { get; }

		/// <summary>Called every frame with the sampled camera (only when a camera track exists).</summary>
		void OnCameraState(in CameraState state);

		/// <summary>Called every frame with the cinematic screen overlay state: letterbox bar amount
		/// (0..1) and fade-to-black alpha (0..1, 1 = fully black).</summary>
		void OnScreen(float letterbox, float fadeAlpha);

		void OnSubtitles(List<SubtitleState> subtitles);

		void OnAudio(string soundEvent, float volume, bool loop);

		void OnEntityVisibility(GameEntityRef entity, bool visible);

		void OnAgentAnimation(string role, string actionName, string facialAnimation, bool loop);

		void OnEventActions(List<ActionBase> actions);

		/// <summary>Called once when playback finishes (or is skipped/stopped).</summary>
		void OnFinished();
	}

	/// <summary>
	/// Resolves <see cref="CinematicTarget"/>s to world frames for the local machine: viewer targets
	/// against the local player (or editor preview), specific agents through the server-resolved map
	/// carried by PlayCinematicMessage, entities through the BuildSystem marker index.
	/// </summary>
	public interface ICinematicBindings
	{
		/// <summary>Full world frame of the target, or null when it cannot be resolved (the player then
		/// falls back to the previous resolved frame).</summary>
		MatrixFrame? ResolveTargetFrame(CinematicTarget target);
	}
}
