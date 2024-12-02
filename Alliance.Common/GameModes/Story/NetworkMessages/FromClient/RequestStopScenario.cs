using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestStopScenario : GameNetworkMessage
	{
		public RequestStopScenario() { }

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
			return "Request to stop current scenario";
		}
	}
}
