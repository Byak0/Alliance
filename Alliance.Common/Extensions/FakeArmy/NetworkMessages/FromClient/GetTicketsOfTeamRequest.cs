using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class GetTicketsOfTeamRequest : GameNetworkMessage
	{

		public GetTicketsOfTeamRequest()
		{
		}

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
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Get number of tickets from one team.";
		}
	}
}
