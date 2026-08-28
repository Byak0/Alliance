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
	/// Resolves role names and the local viewer frame.
	/// </summary>
	public interface ICinematicBindings
	{
		MatrixFrame? ViewerFrame { get; }
		Vec3? ResolveRolePosition(string role);
		Vec3? ResolveEntityPosition(string entityRefId);
	}
}
