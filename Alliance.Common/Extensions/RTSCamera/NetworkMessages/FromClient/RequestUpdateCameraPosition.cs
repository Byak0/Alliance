using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.RTSCamera.NetworkMessages.FromClient
{
	/// <summary>
	/// Request to update camera position.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestUpdateCameraPosition : GameNetworkMessage
	{
		public MatrixFrame MatrixFrame { get; private set; }

		public RequestUpdateCameraPosition() { }

		public RequestUpdateCameraPosition(MatrixFrame matrixFrame)
		{
			MatrixFrame = matrixFrame;
		}

		protected override void OnWrite()
		{
			WriteMatrixFrameToPacket(MatrixFrame);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			MatrixFrame = ReadMatrixFrameFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Peers;
		}

		protected override string OnGetLogFormat()
		{
			return string.Empty;
		}
	}
}
