using Alliance.Common.Extensions.Cinematics;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Behaviors
{
	/// <summary>
	/// Authoritative server-side cinematic timelines.
	/// One PlaybackRecord per running cinematic - multiple cinematics can run concurrently for different audiences.
	/// The server player only consumes <c>EventTrack</c> actions (executed server-side here, ticking their tasks to completion).
	/// Camera/audio/visual tracks are client-side effects.
	/// <para>Registrations arrive from <c>Server_PlayCinematicAction.Execute</c>, which runs on the
	/// parallel entity tick of <c>AL_TriggerAction</c> - hence the concurrent queues, drained on the
	/// main-thread <c>OnMissionTick</c>.</para>
	/// </summary>
	public class CinematicServerBehavior : MissionBehavior
	{
		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

		/// <summary>One running cinematic. Keyed by <see cref="Cinematic.Id"/> (stable per instance).</summary>
		private class PlaybackRecord
		{
			public Cinematic Cinematic;
			public CinematicPlayer Player;
			public VariableStore Context;
			public List<ActionTask> PendingTasks = new List<ActionTask>();
			public bool Finished;
		}

		/// <summary>Server sink: routes EventTrack actions into the record; every visual callback is a no-op.</summary>
		private class ServerCinematicSink : ICinematicPlaybackSink
		{
			private readonly PlaybackRecord _record;
			public ServerCinematicSink(PlaybackRecord record) => _record = record;

			public void OnCameraState(in CameraState state) { }
			public void OnScreen(float letterbox, float fadeAlpha) { }
			public void OnSubtitles(List<SubtitleState> subtitles) { }
			public void OnAudio(string soundEvent, float volume, bool loop) { }
			public void OnEntityVisibility(GameEntityRef entity, bool visible) { }
			public void OnAgentAnimation(string role, string actionName, string facialAnimation, bool loop) { }

			public void OnEventActions(List<ActionBase> actions)
			{
				if (actions == null) return;
				foreach (ActionBase action in actions)
				{
					try
					{
						ActionTask task = action?.Execute(_record.Context);
						if (task != null && !task.IsCompleted) _record.PendingTasks.Add(task);
					}
					catch (Exception ex)
					{
						Log($"Cinematic event action failed on server: {ex.Message}", LogLevel.Warning);
					}
				}
			}

			public void OnFinished() => _record.Finished = true;
		}

		private readonly ConcurrentQueue<Cinematic> _pendingStarts = new ConcurrentQueue<Cinematic>();
		private readonly ConcurrentQueue<string> _pendingStops = new ConcurrentQueue<string>();
		private readonly ConcurrentQueue<(string Id, float Time)> _pendingSeeks = new ConcurrentQueue<(string, float)>();
		private readonly List<PlaybackRecord> _records = new List<PlaybackRecord>();

		/// <summary>
		/// Registers a server playback record (thread-safe). Replaces any record already running
		/// with the same cinematic Id. The cinematic instance itself must already be broadcast to
		/// its audience by the caller (<c>Server_PlayCinematicAction</c>).
		/// </summary>
		public void Play(Cinematic cinematic)
		{
			if (cinematic != null) _pendingStarts.Enqueue(cinematic);
		}

		/// <summary>
		/// Stops the running cinematic with the given Id and broadcasts <see cref="StopCinematicMessage"/>
		/// (thread-safe). An empty id stops every running cinematic (wildcard, e.g. scenario abort).
		/// </summary>
		public void Stop(string cinematicId, bool broadcast = true)
		{
			_pendingStops.Enqueue(cinematicId ?? string.Empty);
			if (broadcast)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new StopCinematicMessage(cinematicId ?? string.Empty));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}

		/// <summary>
		/// Seeks the running cinematic with the given Id to an absolute time and broadcasts
		/// <see cref="SetCinematicTimeMessage"/> so receiving clients follow (thread-safe). No-op when
		/// that cinematic is not running server-side. Future use: admin tools / late-join resync.
		/// </summary>
		public void Seek(string cinematicId, float timeInSeconds, bool broadcast = true)
		{
			if (string.IsNullOrEmpty(cinematicId)) return;
			_pendingSeeks.Enqueue((cinematicId, timeInSeconds));
			if (broadcast)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetCinematicTimeMessage(cinematicId, timeInSeconds));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}

		public override void OnMissionTick(float dt)
		{
			DrainPending();

			for (int i = _records.Count - 1; i >= 0; i--)
			{
				PlaybackRecord record = _records[i];
				if (!record.Finished) record.Player.Tick(dt);
				TickPendingTasks(record, dt);
				if (record.Finished) _records.RemoveAt(i);
			}
		}

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			_records.Clear();
			_pendingStarts.Clear();
			_pendingStops.Clear();
		}

		private void DrainPending()
		{
			while (_pendingStops.TryDequeue(out string stopId))
			{
				// Empty id = wildcard: stop everything (mirrors the client-side StopCinematicMessage semantics).
				if (string.IsNullOrEmpty(stopId))
				{
					_records.Clear();
					continue;
				}
				for (int i = _records.Count - 1; i >= 0; i--)
					if (_records[i].Cinematic.Id == stopId)
						_records.RemoveAt(i);
			}

			while (_pendingSeeks.TryDequeue(out (string Id, float Time) seek))
			{
				foreach (PlaybackRecord record in _records)
				{
					if (record.Cinematic.Id == seek.Id)
					{
						record.Player.Seek(seek.Time);
						break;
					}
				}
			}

			while (_pendingStarts.TryDequeue(out Cinematic cinematic))
			{
				// Same Id already running → replace (clients restart on the new PlayCinematicMessage anyway).
				for (int i = _records.Count - 1; i >= 0; i--)
					if (_records[i].Cinematic.Id == cinematic.Id)
						_records.RemoveAt(i);

				var record = new PlaybackRecord
				{
					Cinematic = cinematic,
					Context = new VariableStore()
				};
				record.Player = new CinematicPlayer(cinematic, new ServerCinematicSink(record), bindings: null);
				record.Player.Start();
				if (record.Player.IsPlaying)
					_records.Add(record);
			}
		}

		private static void TickPendingTasks(PlaybackRecord record, float dt)
		{
			for (int i = record.PendingTasks.Count - 1; i >= 0; i--)
			{
				ActionTask task = record.PendingTasks[i];
				try
				{
					task.Tick(dt, record.Context);
				}
				catch (Exception ex)
				{
					Log($"Cinematic event task failed on server: {ex.Message}", LogLevel.Warning);
					record.PendingTasks.RemoveAt(i);
					continue;
				}
				if (task.IsCompleted) record.PendingTasks.RemoveAt(i);
			}
		}
	}
}
