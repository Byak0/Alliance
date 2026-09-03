using Alliance.Common.Extensions.Cinematics;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Behaviors
{
	/// <summary>
	/// Authoritative server-side cinematic timelines. One record per running cinematic;
	/// several can run concurrently for different audiences.
	/// The server player only consumes EventTrack actions - 
	/// camera/audio/visual tracks are client-side effects.
	/// Records also carry the broadcast info so late joiners receive the cinematic in sync.
	/// Registrations arrive from the parallel entity tick of AL_TriggerAction, hence the
	/// concurrent queues drained on the main-thread OnMissionTick.
	/// </summary>
	public class CinematicServerBehavior : MissionNetwork, IMissionBehavior
	{
		/// <summary>Below this remaining time (non-looping), late joiners are no longer sent the cinematic.</summary>
		private const float LateJoinMinRemainingSec = 5f;

		/// <summary>One running cinematic. Keyed by cinematic name.</summary>
		private class PlaybackRecord
		{
			public Cinematic Cinematic;
			public CinematicPlayer Player;
			public VariableStore Context;
			public PlayCinematicMessage Broadcast;
			/// <summary>Initial audience peers (Players scope: fixed set; Team/All: snapshot for bookkeeping).</summary>
			public HashSet<NetworkCommunicator> AudiencePeers;
			public HashSet<NetworkCommunicator> SentTo = new HashSet<NetworkCommunicator>();
			public List<ActionTask> PendingTasks = new List<ActionTask>();
			public bool Finished;
		}

		/// <summary>Server sink: routes EventTrack actions into the record; every visual callback is a no-op.</summary>
		private class ServerCinematicSink : ICinematicPlaybackSink
		{
			private readonly PlaybackRecord _record;
			public ServerCinematicSink(PlaybackRecord record) => _record = record;

			public bool RequiresVisualSampling => false;

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

		private readonly ConcurrentQueue<(Cinematic Cinematic, PlayCinematicMessage Message, List<NetworkCommunicator> Peers)> _pendingStarts = new ConcurrentQueue<(Cinematic, PlayCinematicMessage, List<NetworkCommunicator>)>();
		private readonly ConcurrentQueue<string> _pendingStops = new ConcurrentQueue<string>();
		private readonly ConcurrentQueue<(string Id, float Time)> _pendingSeeks = new ConcurrentQueue<(string, float)>();
		private readonly List<PlaybackRecord> _records = new List<PlaybackRecord>();
		private readonly Dictionary<Agent, Agent.MortalityState> _savedMortality = new Dictionary<Agent, Agent.MortalityState>();

		/// <summary>Registers a server playback record (thread-safe), replacing any record with the
		/// same cinematic name. The broadcast message is kept to re-send to late joiners.</summary>
		public void Play(Cinematic cinematic, PlayCinematicMessage broadcast, List<NetworkCommunicator> audiencePeers)
		{
			if (cinematic != null && broadcast != null)
				_pendingStarts.Enqueue((cinematic, broadcast, audiencePeers));
		}

		/// <summary>
		/// Stops the running cinematic with the given name and broadcasts <see cref="StopCinematicMessage"/>
		/// (thread-safe). An empty name stops every running cinematic (wildcard, e.g. scenario abort).
		/// </summary>
		public void Stop(string cinematicName, bool broadcast = true)
		{
			_pendingStops.Enqueue(cinematicName ?? string.Empty);
			if (broadcast)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new StopCinematicMessage(cinematicName ?? string.Empty));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}

		/// <summary>
		/// Seeks the running cinematic with the given Id to an absolute time and broadcasts
		/// <see cref="SetCinematicTimeMessage"/> so receiving clients follow (thread-safe). No-op when
		/// that cinematic is not running server-side. Future use: admin tools / late-join resync.
		/// </summary>
		public void Seek(string cinematicName, float timeInSeconds, bool broadcast = true)
		{
			if (string.IsNullOrEmpty(cinematicName)) return;
			_pendingSeeks.Enqueue((cinematicName, timeInSeconds));
			if (broadcast)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SetCinematicTimeMessage(cinematicName, timeInSeconds));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}

		public override void OnMissionTick(float dt)
		{
			DrainPending();

			for (int i = _records.Count - 1; i >= 0; i--)
			{
				PlaybackRecord record = _records[i];
				if (!record.Finished)
				{
					record.Player.Tick(dt);
					TickPendingTasks(record, dt);
					SendToLateJoiners(record);
				}
				if (record.Finished) RemoveRecordAt(i);
			}
		}

		/// <summary>Agents spawning while an invulnerability cinematic runs (respawns, reinforcements)
		/// must be covered too.</summary>
		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			base.OnAgentBuild(agent, banner);
			if (_savedMortality.Count > 0 || HasInvulnerabilityRecords()) RefreshInvulnerability();
		}

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			foreach (KeyValuePair<Agent, Agent.MortalityState> kv in _savedMortality) RestoreMortality(kv.Key, kv.Value);
			_records.Clear();
			_pendingStarts.Clear();
			_pendingStops.Clear();
			_savedMortality.Clear();
		}

		private void DrainPending()
		{
			while (_pendingStops.TryDequeue(out string stopName))
			{
				// Empty id = wildcard: stop everything (mirrors the client-side StopCinematicMessage semantics).
				if (string.IsNullOrEmpty(stopName))
				{
					ClearRecords();
					continue;
				}
				for (int i = _records.Count - 1; i >= 0; i--)
					if (_records[i].Cinematic.Name == stopName)
						RemoveRecordAt(i);
			}

			while (_pendingSeeks.TryDequeue(out (string Id, float Time) seek))
			{
				foreach (PlaybackRecord record in _records)
				{
					if (record.Cinematic.Name == seek.Id)
					{
						record.Player.Seek(seek.Time);
						break;
					}
				}
			}

			while (_pendingStarts.TryDequeue(out (Cinematic Cinematic, PlayCinematicMessage Message, List<NetworkCommunicator> Peers) start))
			{
				// Same Id already running ? replace (clients restart on the new PlayCinematicMessage anyway).
				for (int i = _records.Count - 1; i >= 0; i--)
					if (_records[i].Cinematic.Name == start.Cinematic.Name)
						RemoveRecordAt(i);

				var record = new PlaybackRecord
				{
					Cinematic = start.Cinematic,
					Context = new VariableStore(),
					Broadcast = start.Message,
					AudiencePeers = new HashSet<NetworkCommunicator>(start.Peers ?? new List<NetworkCommunicator>())
				};
				// The All scope was sent through a broadcast: remember every currently synchronized peer
				// as the initial audience so the late-join logic only targets actual newcomers.
				if ((start.Cinematic.Audience?.Scope ?? AudienceScope.All) == AudienceScope.All)
				{
					foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
						if (peer?.IsSynchronized == true) record.AudiencePeers.Add(peer);
				}
				record.Player = new CinematicPlayer(start.Cinematic, new ServerCinematicSink(record), bindings: null);
				record.Player.Start();
				if (record.Player.IsPlaying)
					_records.Add(record);
				RefreshInvulnerability();
			}
		}

		/// <summary>Sends the cinematic to peers who missed the initial broadcast: any newly synchronized
		/// peer for All, peers now on the targeted side for Team (Players is fixed at start). Skipped when
		/// less than LateJoinMinRemainingSec remains: not worth interrupting a fresh joiner.</summary>
		private void SendToLateJoiners(PlaybackRecord record)
		{
			Cinematic cinematic = record.Cinematic;
			if (!cinematic.Loop && record.Player.Duration - record.Player.CurrentTime < LateJoinMinRemainingSec) return;

			AudienceScope scope = cinematic.Audience?.Scope ?? AudienceScope.All;
			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				if (peer == null || !peer.IsSynchronized || record.SentTo.Contains(peer) || record.AudiencePeers.Contains(peer)) continue;
				if (!MatchesAudience(peer, scope, cinematic.Audience)) continue;
				SendToPeer(record.Broadcast, peer);
				record.SentTo.Add(peer);
			}
		}

		private static bool MatchesAudience(NetworkCommunicator peer, AudienceScope scope, Audience audience)
		{
			MissionPeer mp = peer.GetComponent<MissionPeer>();
			switch (scope)
			{
				case AudienceScope.All:
					return true;
				case AudienceScope.Team:
					return mp?.Team != null && mp.Team.Side == audience.Team;
				default:
					return false;
			}
		}

		private static void SendToPeer(PlayCinematicMessage message, NetworkCommunicator peer)
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(message);
			GameNetwork.EndModuleEventAsServer();
		}

		private void RemoveRecordAt(int index)
		{
			_records.RemoveAt(index);
			RefreshInvulnerability();
		}

		private void ClearRecords()
		{
			_records.Clear();
			RefreshInvulnerability();
		}

		private bool HasInvulnerabilityRecords()
		{
			foreach (PlaybackRecord record in _records)
				if (record.Cinematic.Invulnerability != InvulnerabilityMode.None) return true;
			return false;
		}

		/// <summary>Applies the combined invulnerability of all running cinematics, mission-wide (damage
		/// is server-authoritative). Newly built agents are covered via OnAgentBuild; uncovered agents
		/// get their saved mortality state back.</summary>
		private void RefreshInvulnerability()
		{
			bool players = false, bots = false, all = false;
			foreach (PlaybackRecord record in _records)
			{
				switch (record.Cinematic.Invulnerability)
				{
					case InvulnerabilityMode.Players: players = true; break;
					case InvulnerabilityMode.Bots: bots = true; break;
					case InvulnerabilityMode.All: all = true; break;
				}
			}

			if (!players && !bots && !all)
			{
				foreach (KeyValuePair<Agent, Agent.MortalityState> kv in _savedMortality) RestoreMortality(kv.Key, kv.Value);
				_savedMortality.Clear();
				return;
			}

			foreach (Agent agent in Mission.Agents)
			{
				if (_savedMortality.ContainsKey(agent) || !IsCoveredByInvulnerability(agent, players, bots, all)) continue;
				_savedMortality[agent] = agent.CurrentMortalityState;
				agent.SetMortalityState(Agent.MortalityState.Invulnerable);
			}

			List<Agent> uncovered = null;
			foreach (KeyValuePair<Agent, Agent.MortalityState> kv in _savedMortality)
			{
				if (!IsCoveredByInvulnerability(kv.Key, players, bots, all) || !Mission.Agents.Contains(kv.Key))
					(uncovered ??= new List<Agent>()).Add(kv.Key);
			}
			if (uncovered != null)
			{
				foreach (Agent agent in uncovered)
				{
					RestoreMortality(agent, _savedMortality[agent]);
					_savedMortality.Remove(agent);
				}
			}
		}

		/// <summary>Mounts are covered through their rider: a protected rider on an unprotected horse would
		/// just fall off when the horse dies. Non-player, non-mount agents count as bots.</summary>
		private static bool IsCoveredByInvulnerability(Agent agent, bool players, bool bots, bool all)
		{
			if (all) return true;
			if (agent.IsMount) return agent.RiderAgent != null && IsCoveredByInvulnerability(agent.RiderAgent, players, bots, all);
			if (players && agent.IsPlayerControlled) return true;
			return bots && !agent.IsPlayerControlled;
		}

		private static void RestoreMortality(Agent agent, Agent.MortalityState state)
		{
			agent?.SetMortalityState(state);
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
