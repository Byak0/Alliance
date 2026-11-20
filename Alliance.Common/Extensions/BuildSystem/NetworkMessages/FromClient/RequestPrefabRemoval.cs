using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestPrefabRemoval : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer IndexCompressionInfo = new CompressionInfo.Integer(0, 10000, true);

		public int BuildIndex { get; private set; }

		public RequestPrefabRemoval(int buildIndex)
		{
			BuildIndex = buildIndex;
		}

		public RequestPrefabRemoval() { }

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			BuildIndex = ReadIntFromPacket(IndexCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(BuildIndex, IndexCompressionInfo);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return "Request removal of building #" + BuildIndex;
		}
	}
}