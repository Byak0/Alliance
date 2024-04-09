using Alliance.Common.Extensions.ClassLimiter.Models;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.RTSCamera.Views
{
    [DefaultView]
    public class RTSCameraView : MissionView
    {
        private float _lastUpdate;

        public override void OnMissionTick(float dt)
        {
            _lastUpdate += dt;
            if (_lastUpdate < 0.25f) return;
            _lastUpdate = 0;

            if (Agent.Main != null) return;
            MatrixFrame cameraFrame = Mission.GetCameraFrame();
            CameraPositionsModel.Instance.UpdateCameraPosition(cameraFrame.origin);
        }
    }
}
