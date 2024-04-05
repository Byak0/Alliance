using Alliance.Common.Extensions.ClassLimiter.Models;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.RTSCamera.Extension
{
    public static class RTSCameraExtensions
    {
        public static Vec3 GetCameraPosition(this NetworkCommunicator player)
        {            
            return CameraPositionsModel.Instance.CameraPositions[player];
        }
    }
}
