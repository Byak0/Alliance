using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
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

		/// <summary>Instant entity command (SetVisible / Teleport / Fx) - fired once on crossing.
		/// MoveTo is continuous and arrives through <see cref="OnEntityMove"/> instead.</summary>
		void OnEntityAction(EntityActionKeyframe keyframe);

		/// <summary>Called every tick while a MoveTo keyframe is active. t is the eased [0..1]
		/// progress; startFrame was captured when the keyframe first became active.</summary>
		void OnEntityMove(EntityActionKeyframe keyframe, MatrixFrame startFrame, float t);

		/// <summary>Agent staging command. On the server the sink executes it authoritatively against
		/// the resolved track target; in the editor preview the puppet host handles it.</summary>
		void OnAgentAction(AgentActionTrack track, AgentActionKeyframe keyframe);

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

		/// <summary>Entity held by a ValueSource slot (literal marker or rewritten variable), or
		/// <see cref="WeakGameEntity.Invalid"/> when unresolvable on this machine.</summary>
		WeakGameEntity ResolveEntity(ValueSource<WeakGameEntity> slot);
	}
}
