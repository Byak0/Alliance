using TaleWorlds.Library;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// The camera sample produced by <see cref="CinematicPlayer"/> each frame, after interpolating the
	/// active <c>CameraTrack</c> and applying any <c>LookAtTrack</c> override. Consumed by the
	/// <c>CinematicView</c> which writes it to <c>MissionScreen.CustomCamera</c>.
	/// Pure data - no engine dependencies beyond <see cref="MatrixFrame"/>, so it builds on client and server.
	/// </summary>
	public struct CameraState
	{
		public MatrixFrame Frame;
		public float Fov;
		public float Near;
		public float Far;
		public float Roll;
		public bool DoFEnabled;
		public float FocusDistance;
		public float DoFStart;
		public float DoFEnd;
		public float Exposure;

		public static CameraState Lerp(in CameraState a, in CameraState b, float t)
		{
			return new CameraState
			{
				Frame = new MatrixFrame(
					Mat3.Lerp(a.Frame.rotation, b.Frame.rotation, t),
					Vec3.Lerp(a.Frame.origin, b.Frame.origin, t)),
				Fov = a.Fov + (b.Fov - a.Fov) * t,
				Near = a.Near + (b.Near - a.Near) * t,
				Far = a.Far + (b.Far - a.Far) * t,
				Roll = a.Roll + (b.Roll - a.Roll) * t,
				DoFEnabled = b.DoFEnabled,
				FocusDistance = a.FocusDistance + (b.FocusDistance - a.FocusDistance) * t,
				DoFStart = a.DoFStart + (b.DoFStart - a.DoFStart) * t,
				DoFEnd = a.DoFEnd + (b.DoFEnd - a.DoFEnd) * t,
				Exposure = a.Exposure + (b.Exposure - a.Exposure) * t
			};
		}
	}
}
