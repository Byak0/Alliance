using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncPrefabRemoval : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer IndexCompressionInfo = new CompressionInfo.Integer(0, 10000, true);

		public int BuildIndex { get; private set; }

		public SyncPrefabRemoval(int buildIndex)
		{
			BuildIndex = buildIndex;
		}

		public SyncPrefabRemoval() { }

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
			return "Sync removal of building #" + BuildIndex;
		}
	}
}