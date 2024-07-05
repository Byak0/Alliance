using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class GetTicketsOfTeamAnswerMessage : GameNetworkMessage
	{
		public int NbTickets { get; private set; }

		public GetTicketsOfTeamAnswerMessage() { }

		public GetTicketsOfTeamAnswerMessage(int nbTickets)
		{
			NbTickets = nbTickets;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(NbTickets, CompressionBasic.GUIDIntCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			NbTickets = ReadIntFromPacket(CompressionBasic.GUIDIntCompressionInfo, ref bufferReadValid);
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
