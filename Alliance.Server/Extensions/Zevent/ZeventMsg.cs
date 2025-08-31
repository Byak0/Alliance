using Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.Zevent
{
	internal static class ZeventMsg
	{
		public static void RequestClientsToUpdateGoldPile(float targetAmount)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ZEventUpdatePileNetworkServerMessage(targetAmount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public static void RequestClientToUpdateGoldPile(float targetAmount, NetworkCommunicator target)
		{
			GameNetwork.BeginModuleEventAsServer(target);
			GameNetwork.WriteMessage(new ZEventUpdatePileNetworkServerMessage(targetAmount));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
