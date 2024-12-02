using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestSetWinner : GameNetworkMessage
	{
		public BattleSideEnum Winner { get; private set; }

		public RequestSetWinner() { }

		public RequestSetWinner(string winner)
		{
			Winner = (BattleSideEnum)System.Enum.Parse(typeof(BattleSideEnum), winner);
		}

		protected override void OnWrite()
		{
			WriteIntToPacket((int)Winner, new CompressionInfo.Integer(-1, 2, true));
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Winner = (BattleSideEnum)ReadIntFromPacket(new CompressionInfo.Integer(-1, 2, true), ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return "Request to set winner of scenario";
		}
	}
}
