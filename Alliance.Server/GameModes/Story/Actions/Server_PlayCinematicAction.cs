using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Server.GameModes.Story.Behaviors;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Server override: broadcasts a PlayCinematicMessage to the audience (by Id for
	/// scenario-scoped cinematics, by action ref for inline ones) and registers a
	/// server-side timeline in CinematicServerBehavior so EventTrack actions fire server-side.
	/// </summary>
	[OverrideAction(typeof(PlayCinematicAction))]
	public class Server_PlayCinematicAction : PlayCinematicAction
	{
		public Server_PlayCinematicAction() : base() { }

		public override ActionTask Execute(VariableStore context)
		{
			Cinematic cinematic = GetCinematic(context);
			if (cinematic == null)
			{
				Log($"PlayCinematicAction: no cinematic found (CinematicRef='{CinematicRef?.CinematicId}', inline={(Cinematic != null)}).", LogLevel.Warning);
				return ActionTask.CompletedTask;
			}

			Broadcast(cinematic);
			RegisterServerPlayback(cinematic);
			return ActionTask.CompletedTask;
		}

		private void Broadcast(Cinematic cinematic)
		{
			Audience audience = cinematic.Audience ?? new Audience();
			long startTicks = MissionTime.Now.NumberOfTicks;
			bool useViewerOrigin = audience.Scope == AudienceScope.RelativeToViewer;

			// By-Id addressing only works for scenario-scoped cinematics; the inline copy is reached
			// through the (ScopeId, ActionId) action registry, which every client builds from the
			// AL_TriggerAction chunks it deserializes itself.
			bool byId = Cinematic == null && CinematicRef != null && !CinematicRef.IsEmpty;
			var message = byId
				? new PlayCinematicMessage(cinematic.Id, startTicks, cinematic.IsSkippable, useViewerOrigin)
				: new PlayCinematicMessage(ScopeId, ActionId, startTicks, cinematic.IsSkippable, useViewerOrigin);

			switch (audience.Scope)
			{
				case AudienceScope.All:
				case AudienceScope.RelativeToViewer:
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(message);
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
					break;

				case AudienceScope.Team:
					foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
					{
						MissionPeer mp = peer?.GetComponent<MissionPeer>();
						if (mp?.Team != null && mp.Team.Side == audience.Team)
						{
							SendToPeer(peer, message);
						}
					}
					break;

				case AudienceScope.Players:
					System.Collections.Generic.List<string> names = audience.GetPlayerNameList();
					if (names.Count == 0) break;
					foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
					{
						MissionPeer mp = peer?.GetComponent<MissionPeer>();
						if (mp != null && names.Contains(mp.Name))
						{
							SendToPeer(peer, message);
						}
					}
					break;
			}
		}

		private static void RegisterServerPlayback(Cinematic cinematic)
		{
			CinematicServerBehavior behavior = Mission.Current?.GetMissionBehavior<CinematicServerBehavior>();
			if (behavior == null)
			{
				Log("PlayCinematicAction: no CinematicServerBehavior in this mission - server-side event actions will not fire.", LogLevel.Warning);
				return;
			}
			behavior.Play(cinematic);
		}

		private static void SendToPeer(NetworkCommunicator peer, PlayCinematicMessage message)
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(message);
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
