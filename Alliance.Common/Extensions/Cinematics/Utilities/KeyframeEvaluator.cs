using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Interpolation helpers for cinematic keyframes: linear, constant (step), smoothstep easing,
	/// and Catmull-Rom paths for camera positions.
	/// </summary>
	public static class KeyframeEvaluator
	{
		public static float Smoothstep(float t) => t * t * (3f - 2f * t);

		public static float Smootherstep(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);

		/// <summary>Eases a raw [0,1] segment parameter according to the interpolation mode.</summary>
		public static float CurveT(Interpolation interp, float t)
		{
			switch (interp)
			{
				case Interpolation.Linear: return t;
				case Interpolation.CatmullRom: return Smoothstep(t);
				case Interpolation.Constant: return 0f;
				default: return Smoothstep(t);
			}
		}

		/// <summary>Standard Catmull-Rom spline interpolation for a Vec3 control polyline.</summary>
		public static Vec3 CatmullRom(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, float t, float tension = 0f)
		{
			float k = 0.5f * (1f - tension);
			Vec3 m1 = k * (p2 - p0);
			Vec3 m2 = k * (p3 - p1);
			float t2 = t * t;
			float t3 = t2 * t;
			return (2*t3 - 3*t2 + 1) * p1 + (t3 - 2*t2 + t) * m1 + (-2*t3 + 3*t2) * p2 + (t3 - t2) * m2;
		}

		/// <summary>
		/// Finds the keyframes bracketing the given time. before/after are the neighbours
		/// (clamped at the ends) used for Catmull-Rom. Returns false when the list is empty.
		/// </summary>
		public static bool Bracket<T>(IList<T> keyframes, float time,
			out T prev, out T next, out T before, out T after, out float localT) where T : CinematicKeyframe
		{
			prev = next = before = after = null;
			localT = 0f;
			if (keyframes == null || keyframes.Count == 0) return false;

			if (time <= keyframes[0].Time || keyframes.Count == 1)
			{
				prev = next = before = after = keyframes[0];
				return true;
			}
			int last = keyframes.Count - 1;
			if (time >= keyframes[last].Time)
			{
				prev = next = before = after = keyframes[last];
				return true;
			}

			int i = 0;
			while (i < last && keyframes[i + 1].Time < time) i++;
			// now keyframes[i].Time <= time <= keyframes[i+1].Time
			prev = keyframes[i];
			next = keyframes[i + 1];
			before = i > 0 ? keyframes[i - 1] : prev;
			after = (i + 2) <= last ? keyframes[i + 2] : next;

			float span = next.Time - prev.Time;
			localT = span > 0.000001f ? (time - prev.Time) / span : 0f;
			if (localT < 0f) localT = 0f;
			if (localT > 1f) localT = 1f;
			return true;
		}

		/// <summary>
		/// Samples a camera keyframe segment into a CameraState. prev/next delimit the segment;
		/// before/after are the outer neighbours the Catmull-Rom position path needs to bend smoothly
		/// through prev/next (clamped at track ends). All other values blend between prev and next only.
		/// </summary>
		public static CameraState EvaluateCamera(
			CameraKeyframe prev, CameraKeyframe next, float localT,
			MatrixFrame prevFrame, MatrixFrame nextFrame, MatrixFrame beforeFrame, MatrixFrame afterFrame)
		{
			// Single keyframe (segment endpoints identical).
			if (ReferenceEquals(prev, next))
			{
				return CameraFromKeyframe(prev, prevFrame);
			}

			Interpolation interp = next.Interpolation;
			bool constant = interp == Interpolation.Constant;
			float curveT = CurveT(interp, localT);

			Vec3 origin;
			if (interp == Interpolation.CatmullRom)
			{
				origin = CatmullRom(beforeFrame.origin, prevFrame.origin, nextFrame.origin, afterFrame.origin, localT, next.Tension);
			}
			else if (constant)
			{
				origin = prevFrame.origin;
			}
			else
			{
				origin = Vec3.Lerp(prevFrame.origin, nextFrame.origin, curveT);
			}

			Mat3 rotation = constant
				? prevFrame.rotation
				: Mat3.Lerp(prevFrame.rotation, nextFrame.rotation, curveT);

			CameraState state = new CameraState
			{
				Frame = new MatrixFrame(rotation, origin),
				Fov = constant ? prev.Fov : prev.Fov + (next.Fov - prev.Fov) * curveT,
				Near = constant ? prev.NearPlane : prev.NearPlane + (next.NearPlane - prev.NearPlane) * curveT,
				Far = constant ? prev.FarPlane : prev.FarPlane + (next.FarPlane - prev.FarPlane) * curveT,
				Roll = constant ? prev.Roll : prev.Roll + (next.Roll - prev.Roll) * curveT,
				DoFEnabled = next.DoFEnabled,
				FocusDistance = constant ? prev.FocusDistance : prev.FocusDistance + (next.FocusDistance - prev.FocusDistance) * curveT,
				DoFStart = constant ? prev.DoFStart : prev.DoFStart + (next.DoFStart - prev.DoFStart) * curveT,
				DoFEnd = constant ? prev.DoFEnd : prev.DoFEnd + (next.DoFEnd - prev.DoFEnd) * curveT,
				Exposure = constant ? prev.Exposure : prev.Exposure + (next.Exposure - prev.Exposure) * curveT
			};
			return state;
		}

		public static CameraState CameraFromKeyframe(CameraKeyframe kf, MatrixFrame frame)
		{
			return new CameraState
			{
				Frame = frame,
				Fov = kf.Fov,
				Near = kf.NearPlane,
				Far = kf.FarPlane,
				Roll = kf.Roll,
				DoFEnabled = kf.DoFEnabled,
				FocusDistance = kf.FocusDistance,
				DoFStart = kf.DoFStart,
				DoFEnd = kf.DoFEnd,
				Exposure = kf.Exposure
			};
		}
	}
}
