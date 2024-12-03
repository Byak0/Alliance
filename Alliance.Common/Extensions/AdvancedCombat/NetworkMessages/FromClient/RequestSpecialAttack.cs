using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdvancedCombat.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestSpecialAttack : GameNetworkMessage
	{
		public RequestSpecialAttack() { }

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
			return "Request usage of SpecialAttack.";
		}
	}
}
