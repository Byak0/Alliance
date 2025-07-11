using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.RTSCamera.Models;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.RTSCamera.Views
{
	[DefaultView]
	public class RTSCameraView : MissionView
	{
		private float _lastUpdate;

		public void ForceCameraToFrame(SetClientCameraFrame frame)
		{
			if (Mission.Current.MainAgent == null)
			{
				Log("Force camera to frame: " + frame.CameraTargetFrame.ToString(), LogLevel.Debug);
				MissionScreen.UpdateFreeCamera(frame.CameraTargetFrame);
			}
		}

		public override void OnMissionTick(float dt)
		{
			_lastUpdate += dt;
			if (_lastUpdate < 0.25f) return;
			_lastUpdate = 0;

			if (Agent.Main != null) return;
			MatrixFrame cameraFrame = Mission.GetCameraFrame();
			CameraPositionsModel.Instance.UpdateCameraPosition(cameraFrame);
		}
	}
}
