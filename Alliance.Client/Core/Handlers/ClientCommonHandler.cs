using Alliance.Client.Extensions.RTSCamera.Views;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Core.Handlers
{
	public class ClientCommonHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SetClientCameraFrame>(HandleClientCameraFrame);
		}

		private void HandleClientCameraFrame(SetClientCameraFrame setClientCameraFrameMsg)
		{
			var rtsCameraView = Mission.Current?.GetMissionBehavior<RTSCameraView>();

			if (rtsCameraView == null)
			{
				return;
			}

			rtsCameraView.ForceCameraToFrame(setClientCameraFrameMsg);
			return;
		}
	}
}
