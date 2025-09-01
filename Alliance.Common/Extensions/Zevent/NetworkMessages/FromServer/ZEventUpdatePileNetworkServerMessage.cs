using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class ZEventUpdatePileNetworkServerMessage : GameNetworkMessage
	{
		public int GoldPileTarget { get; private set; }

		readonly CompressionInfo.Integer CompressionInfo = new CompressionInfo.Integer(0, 20_000_000, true);

		public ZEventUpdatePileNetworkServerMessage() { }

		public ZEventUpdatePileNetworkServerMessage(int target)
		{
			GoldPileTarget = target;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(GoldPileTarget, CompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;

			GoldPileTarget = ReadIntFromPacket(CompressionInfo, ref bufferReadValid);

			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Send to client to update his gold pile";
		}
	}
}
