using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SetClientCameraFrame : GameNetworkMessage
	{
		public MatrixFrame CameraTargetFrame { get; set; }

		public SetClientCameraFrame()
		{
		}

		public SetClientCameraFrame(MatrixFrame cameraTargetFrame)
		{
			CameraTargetFrame = cameraTargetFrame;
		}

		protected override void OnWrite()
		{
			WriteMatrixFrameToPacket(CameraTargetFrame);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			CameraTargetFrame = ReadMatrixFrameFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Agents;
		}
		protected override string OnGetLogFormat()
		{
			return "Setting client camera frame";
		}
	}
}
