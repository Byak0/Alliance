using Alliance.Common.Extensions.RTSCamera.Models;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.RTSCamera.Extension
{
	public static class RTSCameraExtensions
	{
		public static Vec3 GetCameraPosition(this NetworkCommunicator player)
		{
			var found = CameraPositionsModel.Instance.CameraPositions.TryGetValue(player, out MatrixFrame pos);
			if (!found || !pos.origin.IsValid)
			{
				return Vec3.Invalid;
			};

			return pos.origin;
		}

		public static MatrixFrame GetCameraFrame(this NetworkCommunicator player)
		{
			var found = CameraPositionsModel.Instance.CameraPositions.TryGetValue(player, out MatrixFrame pos);
			if (!found)
			{
				return MatrixFrame.Zero;
			};

			return pos;
		}
	}
}
