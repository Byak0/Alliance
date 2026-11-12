using TaleWorlds.Library;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Camera math helpers. Bannerlord cameras look along -rotation.u, so a look-at must set
	/// u = -forward (same construction as native PopupSceneCameraPath.CreateLookAt).
	/// </summary>
	public static class CameraMath
	{
		/// <summary>Builds a camera frame at the given position looking at the target.</summary>
		public static MatrixFrame LookAtFrame(Vec3 position, Vec3 target, Vec3? upVector = null)
		{
			Vec3 up = upVector ?? new Vec3(0f, 0f, 1f);

			Vec3 forward = target - position;
			forward.Normalize();

			Vec3 side = Vec3.CrossProduct(forward, up);
			side.Normalize();
			Vec3 realUp = Vec3.CrossProduct(side, forward);

			// s = side, f = realUp, u = -forward  (so Camera.Direction = -u = forward)
			Mat3 rotation = new Mat3(side, realUp, -forward);
			return new MatrixFrame(rotation, position);
		}
	}
}
