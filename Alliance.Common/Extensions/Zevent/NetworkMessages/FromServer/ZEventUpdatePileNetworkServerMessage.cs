using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public class ZEventUpdatePileNetworkServerMessage : GameNetworkMessage
	{
		public float GoldPileTarget { get; private set; }

		readonly CompressionInfo.Float CompressionInfo;

		public ZEventUpdatePileNetworkServerMessage(float target)
		{
			GoldPileTarget = target;
			CompressionInfo = new CompressionInfo.Float(0, 20000, 0.1f);
		}

		protected override void OnWrite()
		{
			WriteFloatToPacket(GoldPileTarget, CompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;

			GoldPileTarget = ReadFloatFromPacket(CompressionInfo, ref bufferReadValid);

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
