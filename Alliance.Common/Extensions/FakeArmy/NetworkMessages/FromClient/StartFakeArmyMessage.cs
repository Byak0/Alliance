using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class StartFakeArmyMessage : GameNetworkMessage
	{
		public StartFakeArmyMessage() { }

		protected override void OnWrite()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return "Request to start fake army";
		}
	}
}
