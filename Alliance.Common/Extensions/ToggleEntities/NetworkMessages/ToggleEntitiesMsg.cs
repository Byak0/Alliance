using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromClient;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.ToggleEntities.NetworkMessages
{
	public static class ToggleEntitiesMsg
	{
		/// <summary>
		/// From server - Broadcast tag visibility to all clients.
		/// </summary>
		public static void SyncToggleEntities(string tag, bool visibility)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncToggleEntities(tag, visibility));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// From server - Send tag visibility to a player.
		/// </summary>
		public static void SyncToggleEntities(NetworkCommunicator peer, string tag, bool visibility)
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(new SyncToggleEntities(tag, visibility));
			GameNetwork.EndModuleEventAsServer();
		}

		/// <summary>
		/// From server - Broadcast tag visibility to all clients for a specific mission object and its children only.
		/// </summary>
		public static void SyncToggleEntitiesLocal(string tag, bool visibility, MissionObjectId missionObjectId)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncToggleEntitiesLocal(tag, visibility, missionObjectId));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// From server - Send tag visibility to a player for a specific mission object and its children only.
		/// </summary>
		public static void SyncToggleEntitiesLocal(NetworkCommunicator peer, string tag, bool visibility, MissionObjectId missionObjectId)
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(new SyncToggleEntitiesLocal(tag, visibility, missionObjectId));
			GameNetwork.EndModuleEventAsServer();
		}

		/// <summary>
		/// From client - Request the server to toggle entities with a specific tag visibility.
		/// </summary>
		public static void RequestToggleEntities(string tag, bool visibility)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestToggleEntities(tag, visibility));
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
