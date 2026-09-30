using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.Cinematics;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using Alliance.Common.Extensions.Cinematics.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
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

		/// <summary>Server sink: routes EventTrack actions into the record and executes AgentActionTrack
		/// commands authoritatively; every other visual callback is a no-op.</summary>
		private class ServerCinematicSink : ICinematicPlaybackSink
		{
			private const float Deg2Rad = 0.017453292f;
			private readonly PlaybackRecord _record;
			public ServerCinematicSink(PlaybackRecord record) => _record = record;

			public bool RequiresVisualSampling => false;

			public void OnCameraState(in CameraState state) { }
			public void OnScreen(float letterbox, float fadeAlpha) { }
			public void OnSubtitles(List<SubtitleState> subtitles) { }
			public void OnAudio(string soundEvent, float volume, bool loop) { }
			public void OnEntityAction(EntityActionKeyframe keyframe) { }
			public void OnEntityMove(EntityActionKeyframe keyframe, MatrixFrame startFrame, float t) { }

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

			public void OnAgentAction(AgentActionTrack track, AgentActionKeyframe keyframe)
			{
				try
				{
					ExecuteAgentAction(track, keyframe);
				}
				catch (Exception ex)
				{
					Log($"Cinematic agent action '{keyframe?.Kind}' failed on server: {ex.Message}", LogLevel.Warning);
				}
			}

			public void OnFinished() => _record.Finished = true;

			/// <summary>True-agent mode only: resolves the variable slots and applies the command to
			/// every agent. Locomotion and body animations replicate natively to clients. Staged
			/// extras never reach the server - clients run them deterministically.</summary>
			private void ExecuteAgentAction(AgentActionTrack track, AgentActionKeyframe kf)
			{
				if (track.Target?.IsTrueMode != true) return;

			List<Agent> agents = track.Target.ResolveAgents(_record.Context);
			if (agents.Count == 0)
			{
				Log($"[Cinematic] Agent action '{kf?.Kind}' skipped: the track target resolved to no agent on the server.", LogLevel.Warning);
				return;
			}

			Vec2[] offsets = CaptureFormationOffsets(agents, kf);
			for (int i = 0; i < agents.Count; i++)
			{
				Agent agent = agents[i];
				if (agent == null) continue;
				try
				{
					ExecuteOnAgent(agent, kf, i, offsets);
				}
				catch (Exception ex)
				{
					Log($"Cinematic agent action '{kf.Kind}' failed for an agent: {ex.Message}", LogLevel.Warning);
				}
			}
		}

			/// <summary>Local-space offsets of each agent within its group's current arrangement
			/// (centroid + mean facing), captured once per Teleport/MoveTo keyframe so each agent
			/// keeps its spot around the destination ('Keep formation offset'). Null when the
			/// option is off or the action has no destination.</summary>
			private static Vec2[] CaptureFormationOffsets(List<Agent> agents, AgentActionKeyframe kf)
			{
				if (!kf.KeepFormationOffset || kf.Kind != AgentActionKind.Teleport && kf.Kind != AgentActionKind.MoveTo)
				{
					return null;
				}

				Vec2 center = Vec2.Zero;
				Vec2 facingSum = Vec2.Zero;
				int count = 0;
				foreach (Agent agent in agents)
				{
					if (agent == null) continue;
					center += agent.Position.AsVec2;
					facingSum += agent.Frame.rotation.f.AsVec2;
					count++;
				}
				Vec2[] offsets = new Vec2[agents.Count];
				if (count <= 1) return offsets;

				MatrixFrame groupFrame = MatrixFrame.Identity;
				groupFrame.rotation.ApplyEulerAngles(new Vec3(0f, 0f, facingSum.RotationInRadians));
				groupFrame.origin = new Vec3(center.x / count, center.y / count, 0f);
				for (int i = 0; i < agents.Count; i++)
				{
					if (agents[i] == null) continue;
					Vec3 local = groupFrame.TransformToLocal(agents[i].Position);
					offsets[i] = new Vec2(local.x, local.y);
				}
				return offsets;
			}

			/// <summary>The keyframe destination, plus the agent's captured formation offset
			/// rotated into the destination's orientation (null offsets = exact destination point).</summary>
			private static Vec3 MemberDestination(MatrixFrame frame, int index, Vec2[] offsets)
			{
				return offsets != null
					? frame.TransformToParent(new Vec3(offsets[index].x, offsets[index].y, 0f))
					: frame.origin;
			}

			private void ExecuteOnAgent(Agent agent, AgentActionKeyframe kf, int index, Vec2[] offsets)
			{
				switch (kf.Kind)
				{
					case AgentActionKind.Teleport:
						{
							MatrixFrame? frame = kf.Destination?.ResolveWorldFrame(_record.Context);
							if (!frame.HasValue)
							{
								Log("[Cinematic] Agent Teleport skipped: destination could not be resolved (position/marker/variable/agent).", LogLevel.Warning);
								break;
							}
							Vec3 position = MemberDestination(frame.Value, index, offsets);
							// Mounted agents move as a pair: the mount carries the rider - teleporting
							// the rider alone would leave the mount behind.
							Agent mover = agent.MountAgent ?? agent;
							mover.TeleportToPosition(position);
							ScriptAgentTo(mover, position, frame.Value.rotation.f.AsVec2.RotationInRadians, walk: true);
							break;
						}

					case AgentActionKind.MoveTo:
						{
							MatrixFrame? destination = kf.Destination?.ResolveWorldFrame(_record.Context);
							if (!destination.HasValue)
							{
								Log("[Cinematic] Agent MoveTo skipped: destination could not be resolved (position/marker/variable).", LogLevel.Warning);
								break;
							}
							Vec3 origin = MemberDestination(destination.Value, index, offsets);
							float travelDirection = (origin - agent.Position).AsVec2.RotationInRadians;
							float arrivalFacing = kf.ArrivalFacingDeg >= 0f ? kf.ArrivalFacingDeg * Deg2Rad : travelDirection;
							float? customSpeed = kf.Speed == AgentMoveSpeed.Custom ? Math.Max(0.1f, kf.CustomSpeed) : (float?)null;
							// Scripted locomotion goes to the mount of mounted agents (it carries the rider).
							ScriptAgentTo(agent.MountAgent ?? agent, origin, arrivalFacing, walk: kf.Speed == AgentMoveSpeed.Walk, customSpeed);
							break;
						}

					case AgentActionKind.PlayAnimation:
						// Routed through the AnimationSystem: auto-fixes the agent's action set and
						// broadcasts SyncAnimation so clients replay it reliably.
						if (!string.IsNullOrEmpty(kf.ActionName)
							&& AnimationSystem.Instance.ActionNameToAnimation != null
							&& AnimationSystem.Instance.ActionNameToAnimation.TryGetValue(kf.ActionName, out Animation animation))
						{
							Animation played = new Animation(animation.Index, animation.Name, kf.ActionSpeed, animation.MaxDuration);
							AnimationSystem.Instance.PlayAnimation(agent, played, synchronize: true, loop: kf.Loop, channel: kf.Channel);
						}
						// Optional mount action, played on the mount agent and replicated the same way.
						if (!string.IsNullOrEmpty(kf.MountActionName) && agent.MountAgent != null
							&& AnimationSystem.Instance.ActionNameToAnimation != null
							&& AnimationSystem.Instance.ActionNameToAnimation.TryGetValue(kf.MountActionName, out Animation mountAnimation))
						{
							Animation playedMount = new Animation(mountAnimation.Index, mountAnimation.Name, kf.ActionSpeed, mountAnimation.MaxDuration);
							AnimationSystem.Instance.PlayAnimation(agent.MountAgent, playedMount, synchronize: true, loop: kf.Loop, channel: 0);
						}
						break;

					case AgentActionKind.PlayFacial:
						if (!string.IsNullOrEmpty(kf.FacialAnimName))
							agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, kf.FacialAnimName, kf.FacialLoop);
						break;

					case AgentActionKind.SetVisible:
						// Visuals-only on every machine (server included); clients apply it locally too.
						agent.AgentVisuals?.SetVisible(kf.Visible);
						if (kf.IncludeMount && agent.MountAgent != null) agent.MountAgent.AgentVisuals?.SetVisible(kf.Visible);
						break;
				}
			}

		/// <summary>Orders the native scripted movement (TaleWorlds' cutscene/conversation primitive):
		/// real locomotion with footsteps, avoids obstacles. DoNotRun enforces the walk pace. A custom
		/// speed (m/s) overrides the native speed limit instead; -1f restores the default limit.</summary>
		private static void ScriptAgentTo(Agent agent, Vec3 position, float facingRadians, bool walk, float? customSpeed = null)
		{
			Scene scene = Mission.Current?.Scene;
			if (scene == null) return;
			WorldPosition worldPosition = new WorldPosition(scene, position);
			Agent.AIScriptedFrameFlags flags = walk && !customSpeed.HasValue ? Agent.AIScriptedFrameFlags.DoNotRun : Agent.AIScriptedFrameFlags.None;
			agent.SetScriptedPositionAndDirection(ref worldPosition, facingRadians, false, flags);
			agent.SetMaximumSpeedLimit(customSpeed ?? -1f, isMultiplier: false);
		}
		}

		private readonly ConcurrentQueue<(Cinematic Cinematic, PlayCinematicMessage Message, List<NetworkCommunicator> Peers)> _pendingStarts = new ConcurrentQueue<(Cinematic, PlayCinematicMessage, List<NetworkCommunicator>)>();
		private readonly ConcurrentQueue<string> _pendingStops = new ConcurrentQueue<string>();
		private readonly ConcurrentQueue<(string Id, float Time)> _pendingSeeks = new ConcurrentQueue<(string, float)>();
		private readonly List<PlaybackRecord> _records = new List<PlaybackRecord>();
		private readonly Dictionary<Agent, Agent.MortalityState> _savedMortality = new Dictionary<Agent, Agent.MortalityState>();
		/// <summary>Persistent staged-extras groups (persist=true), synced to late joiners. Key = cinematic name # track index.</summary>
		private readonly Dictionary<string, PersistentFakeGroup> _persistentFakeGroups = new Dictionary<string, PersistentFakeGroup>();
		private readonly HashSet<NetworkCommunicator> _fakeGroupsSynced = new HashSet<NetworkCommunicator>();

		/// <summary>A persist=true staged-extras group standing in the world after its cinematic ended.</summary>
		private class PersistentFakeGroup
		{
			public string Key;
			public string CharacterId;
			public string CultureId;
			public int Count;
			public CinematicFormationLayout Layout;
			public int Rows;
			public float Spacing;
			public bool WeaponsDrawn;
			public MatrixFrame FinalFrame;
		}

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

			SyncFakeGroupsToNewPeers();
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
			// Mission end: entities die with the scene - drop the registries.
			_persistentFakeGroups.Clear();
			_fakeGroupsSynced.Clear();
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
			RegisterPersistentGroups(_records[index]);
			_records.RemoveAt(index);
			RefreshInvulnerability();
		}

		/// <summary>When a cinematic stops/finishes/gets replaced, its persist=true staged tracks
		/// remain standing in the world: register the group (keyed, replacing on replay) so late
		/// joiners receive a snapshot of where the extras stand.</summary>
		private void RegisterPersistentGroups(PlaybackRecord record)
		{
			Cinematic cinematic = record?.Cinematic;
			if (cinematic?.Tracks == null) return;

			for (int i = 0; i < cinematic.Tracks.Count; i++)
			{
				if (cinematic.Tracks[i] is not AgentActionTrack track) continue;
				if (track.Target?.IsStagedMode != true || !track.Target.Persist) continue;

				string key = $"{cinematic.Name}#{i}";
				_persistentFakeGroups[key] = new PersistentFakeGroup
				{
					Key = key,
					CharacterId = track.Target.CharacterId,
					CultureId = track.Target.CultureId,
					Count = track.Target.Count,
					Layout = track.Target.Layout,
					Rows = track.Target.Rows,
				Spacing = track.Target.Spacing,
				WeaponsDrawn = track.Target.WeaponsDrawn,
				// The group stands at its last ordered destination (fallback: authored origin).
				FinalFrame = ComputeFinalStagingFrame(track, record)
				};
				// Everyone gets a fresh snapshot (clients holding this key locally deduplicate).
				_fakeGroupsSynced.Clear();
			}
		}

		private MatrixFrame ComputeFinalStagingFrame(AgentActionTrack track, PlaybackRecord record)
		{
			AgentActionKeyframe last = null;
			foreach (AgentActionKeyframe kf in track.Keyframes ?? Enumerable.Empty<AgentActionKeyframe>())
			{
				if (kf == null || (kf.Kind != AgentActionKind.Teleport && kf.Kind != AgentActionKind.MoveTo)) continue;
				if (last == null || kf.Time >= last.Time) last = kf;
			}
			if (last != null)
			{
				MatrixFrame? frame = last.Destination?.ResolveWorldFrame(record.Context);
				if (frame.HasValue) return frame.Value;
			}
			return track.Target.Origin.ToFrame();
		}

		/// <summary>Sends the persistent-group snapshot to every newly synchronized peer.</summary>
		private void SyncFakeGroupsToNewPeers()
		{
			if (_persistentFakeGroups.Count == 0) return;
			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				if (peer == null || !peer.IsSynchronized || _fakeGroupsSynced.Contains(peer)) continue;
				List<FakeAgentGroupData> groups = new List<FakeAgentGroupData>();
				foreach (PersistentFakeGroup group in _persistentFakeGroups.Values)
				{
					groups.Add(new FakeAgentGroupData
					{
						Key = group.Key,
						CharacterId = group.CharacterId,
						CultureId = group.CultureId,
						Count = group.Count,
						Layout = group.Layout,
						Rows = group.Rows,
					Spacing = group.Spacing,
					WeaponsDrawn = group.WeaponsDrawn,
					X = group.FinalFrame.origin.x,
						Y = group.FinalFrame.origin.y,
						Z = group.FinalFrame.origin.z,
						Yaw = group.FinalFrame.rotation.f.AsVec2.RotationInRadians
					});
				}
				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(new SyncFakeAgentGroups(groups));
				GameNetwork.EndModuleEventAsServer();
				_fakeGroupsSynced.Add(peer);
			}
		}

		private void ClearRecords()
		{
			_records.Clear();
			// Scenario abort resets the world: persistent groups go with it.
			_persistentFakeGroups.Clear();
			_fakeGroupsSynced.Clear();
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
