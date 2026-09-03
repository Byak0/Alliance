using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Server.GameModes.Story.Behaviors;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Server override: broadcasts a PlayCinematicMessage to the audience
	/// (by name for scenario cinematics, by action ref for inline ones)
	/// Registers a server-side timeline in CinematicServerBehavior so EventTrack actions fire server-side
	/// and late joiners receive the cinematic in sync.
	/// </summary>
	[OverrideAction(typeof(PlayCinematicAction))]
	public class Server_PlayCinematicAction : PlayCinematicAction
	{
		public Server_PlayCinematicAction() : base() { }

		public override ActionTask Execute(VariableStore context)
		{
			Cinematic cinematic = GetCinematic();
			if (cinematic == null)
			{
				Log($"PlayCinematicAction: no cinematic found (CinematicName='{CinematicName}').", LogLevel.Warning);
				return ActionTask.CompletedTask;
			}

			List<object> dynamicValues = ResolveDynamicData(cinematic, context);
			PlayCinematicMessage message = BuildMessage(cinematic, dynamicValues);
			List<NetworkCommunicator> audiencePeers = ResolveAudiencePeers(cinematic.Audience, context);

			SendToAudience(message, cinematic.Audience, audiencePeers);
			RegisterServerPlayback(cinematic, message, audiencePeers);
			return ActionTask.CompletedTask;
		}

		// Resolves the cinematic's dynamic slots server-side; the bare values are shipped with the
		// play message and rewritten as Literals on the client.
		private static List<object> ResolveDynamicData(Cinematic cinematic, VariableStore context)
		{
			List<ValueSourceHelper.DynamicSlot> slots = ValueSourceHelper.CollectDynamicSlots(cinematic);
			List<object> values = new List<object>(slots.Count);

			VariableStore globals = ScenarioManager.Instance?.Globals;
			foreach (ValueSourceHelper.DynamicSlot slot in slots)
			{
				ValueSource vs = ValueSourceHelper.GetSlotValueSource(slot);
				object value = null;
				try
				{
					value = vs.ResolveObject(context, globals);
				}
				catch (Exception ex)
				{
					Log($"PlayCinematicAction: dynamic slot failed to resolve - it will fall back ({ex.Message}).", LogLevel.Warning);
				}
				values.Add(value); // nulls are kept: they consume their slot on the client
			}
			return values;
		}

		private PlayCinematicMessage BuildMessage(Cinematic cinematic, List<object> dynamicValues)
		{
			// Anchor = mission time at cinematic start (synced across peers). Each receiver computes
			// its own advancement at receive time, so late joiners catch up without rebuilding the message.
			long startTicks = MissionTime.Now.NumberOfTicks;
			// Inline cinematics are addressed by (ScopeId, ActionId); name-referenced ones by name.
			return UseActionRef
				? new PlayCinematicMessage(ScopeId, ActionId, startTicks, dynamicValues)
				: new PlayCinematicMessage(cinematic.Name, startTicks, dynamicValues);
		}

		/// <summary>Resolves the audience to its initial peer set. Team and All audiences re-evaluate
		/// for late joiners on the server behavior; the Players audience is fixed at start time.</summary>
		private static List<NetworkCommunicator> ResolveAudiencePeers(Audience audience, VariableStore context)
		{
			var peers = new List<NetworkCommunicator>();
			if (audience == null || audience.Scope == AudienceScope.All) return peers;

			switch (audience.Scope)
			{
				case AudienceScope.Team:
					foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
					{
						MissionPeer mp = peer?.GetComponent<MissionPeer>();
						if (mp?.Team != null && mp.Team.Side == audience.Team) peers.Add(peer);
					}
					break;

				case AudienceScope.Players:
					{
						VariableStore globals = ScenarioManager.Instance?.Globals;
						foreach (ValueSource<Agent> variable in audience.PlayerVariables ?? new List<ValueSource<Agent>>())
						{
						Agent agent = variable?.Resolve(context, globals);
						NetworkCommunicator peer = agent?.MissionPeer?.GetNetworkPeer();
						if (peer != null && !peers.Contains(peer)) peers.Add(peer);
						else if (agent == null) Log($"PlayCinematicAction: audience agent slot unresolved ({variable?.GetType().Name}) - that player won't see the cinematic.", LogLevel.Warning);
						}
					}
					break;
			}
			return peers;
		}

		private static void SendToAudience(PlayCinematicMessage message, Audience audience, List<NetworkCommunicator> audiencePeers)
		{
			if (audience == null || audience.Scope == AudienceScope.All)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(message);
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				return;
			}
			foreach (NetworkCommunicator peer in audiencePeers) SendToPeer(peer, message);
		}

		private static void RegisterServerPlayback(Cinematic cinematic, PlayCinematicMessage message, List<NetworkCommunicator> audiencePeers)
		{
			CinematicServerBehavior behavior = Mission.Current?.GetMissionBehavior<CinematicServerBehavior>();
			if (behavior == null)
			{
				Log("PlayCinematicAction: no CinematicServerBehavior in this mission - server-side event actions will not fire.", LogLevel.Error);
				return;
			}
			behavior.Play(cinematic, message, audiencePeers);
		}

		private static void SendToPeer(NetworkCommunicator peer, PlayCinematicMessage message)
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(message);
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
