using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.GameModes.PvC
{
	public static class PvCMsg
	{
		public static void SendSyncGoldMsgToAllPeers(SyncGoldsForSkirmish goldData)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(goldData);
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}
