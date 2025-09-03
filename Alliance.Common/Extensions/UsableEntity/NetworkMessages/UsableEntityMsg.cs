using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromClient;
using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Extensions.UsableEntity.Behaviors.UsableEntityBehavior;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages
{
	public static class UsableEntityMsg
	{
		#region Client messages
		public static void RequestUseEntity(InteractionTarget target)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestUseEntity(target.ID));
			GameNetwork.EndModuleEventAsClient();
		}

		public static void RequestTextPanelEdit(string newText, InteractionTarget target)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestEditTextPanel(target.ID, newText));
			GameNetwork.EndModuleEventAsClient();
		}
		#endregion

		#region Server messages
		public static void SyncHideEntity(InteractionTarget entity, NetworkCommunicator networkPeer = null)
		{
			if (networkPeer == null)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new HideEntity(entity.ID));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			else
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new HideEntity(entity.ID));
				GameNetwork.EndModuleEventAsServer();
			}
		}

		public static void SyncResetEntityVisibility()
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ResetUsableEntityVisibility());
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		#endregion
	}
}
