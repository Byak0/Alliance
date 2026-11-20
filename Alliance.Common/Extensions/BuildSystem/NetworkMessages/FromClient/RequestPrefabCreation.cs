using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestPrefabCreation : GameNetworkMessage
	{
		public string PrefabName { get; private set; }
		public MatrixFrame PrefabFrame { get; private set; }

		public RequestPrefabCreation(string prefabName, MatrixFrame prefabFrame)
		{
			PrefabName = prefabName;
			PrefabFrame = prefabFrame;
		}

		public RequestPrefabCreation() { }

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			PrefabName = ReadStringFromPacket(ref bufferReadValid);
			PrefabFrame = ReadMatrixFrameFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteStringToPacket(PrefabName);
			WriteMatrixFrameToPacket(PrefabFrame);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return "Request building " + PrefabName;
		}
	}
}
