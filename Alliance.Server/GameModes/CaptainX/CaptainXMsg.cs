using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.CaptainX
{
	internal static class CaptainXMsg
	{
		public static void SendCapturedPointMsgToAllPeers(FlagDominationCapturePointMessage captureFlagMsg)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(captureFlagMsg);
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public static void SendCapturedPointMsgToPeer(NetworkCommunicator target, FlagDominationCapturePointMessage captureFlagMsg)
		{
			GameNetwork.BeginModuleEventAsServer(target);
			GameNetwork.WriteMessage(captureFlagMsg);
			GameNetwork.EndModuleEventAsServer();
		}

		public static void SendFlagDominationMoraleChangeMessageToAllPeers(FlagDominationMoraleChangeMessage moraleMsg)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(moraleMsg);
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		public static void SendFlagDominationMoraleChangeMessageToPeer(NetworkCommunicator target, FlagDominationMoraleChangeMessage moralMsg)
		{
			GameNetwork.BeginModuleEventAsServer(target);
			GameNetwork.WriteMessage(moralMsg);
			GameNetwork.EndModuleEventAsServer();
		}

		public static void SendFlagDominationFlagsRemovedMessageToAllPeers(FlagDominationFlagsRemovedMessage removeMsg)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(removeMsg);
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}
