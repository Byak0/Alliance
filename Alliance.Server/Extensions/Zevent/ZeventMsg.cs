using Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.Zevent
{
	internal static class ZeventMsg
	{
		/// <summary>
		/// Need to send the Integer value. So it should NOT be divided by 1000
		/// </summary>
		/// <param name="targetAmount"></param>
		public static void RequestClientsToUpdateGoldPile(int targetAmount)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ZEventUpdatePileNetworkServerMessage(targetAmount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// Need to send the Integer value. So it should NOT be divided by 1000
		/// </summary>
		/// <param name="targetAmount"></param>
		public static void RequestClientToUpdateGoldPile(int targetAmount, NetworkCommunicator target)
		{
			GameNetwork.BeginModuleEventAsServer(target);
			GameNetwork.WriteMessage(new ZEventUpdatePileNetworkServerMessage(targetAmount));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
