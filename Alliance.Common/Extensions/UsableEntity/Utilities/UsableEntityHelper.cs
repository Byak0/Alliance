using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace Alliance.Common.Extensions.UsableEntity.Utilities
{
	public static class UsableEntityHelper
	{
		private const int POSITION_PRECISION = 2; // meters -> 2 decimals (centimeters)
		private const int QUAT_PRECISION = 4; // quaternion -> 4 decimals

		public static Guid GetDeterministicID(this GameEntity entity)
		{
			if (entity == null) return Guid.Empty;
			int siblingIndex = entity.Parent?.GetChildren().FindIndexQ(entity) ?? -1;
			return new Guid(ComputeBytes(entity.Name, entity.GetGlobalFrame(), entity.Tags, siblingIndex));
		}

		public static byte[] ComputeBytes(string name, MatrixFrame frame, string[] tags, int siblingIndex)
		{
			StringBuilder sb = new StringBuilder(192);

			// Name
			sb.Append((name ?? "").Trim().ToLowerInvariant()).Append('|');

			// Position
			sb.Append(FloatToString(frame.origin.x, POSITION_PRECISION)).Append(',');
			sb.Append(FloatToString(frame.origin.y, POSITION_PRECISION)).Append(',');
			sb.Append(FloatToString(frame.origin.z, POSITION_PRECISION)).Append('|');

			// Rotation
			Quaternion r = frame.rotation.ToQuaternion();
			Normalize(ref r);
			Canonicalize(ref r);
			sb.Append(FloatToString(r.W, QUAT_PRECISION)); sb.Append(',');
			sb.Append(FloatToString(r.X, QUAT_PRECISION)); sb.Append(',');
			sb.Append(FloatToString(r.Y, QUAT_PRECISION)); sb.Append(',');
			sb.Append(FloatToString(r.Z, QUAT_PRECISION)); sb.Append('|');

			// Sibling index
			sb.Append(siblingIndex).Append('|');

			// Tags (sorted, lowercased, unique) -> single string
			if (tags != null && tags.Length > 0)
			{
				var canon = tags
					.Where(t => !string.IsNullOrWhiteSpace(t))
					.Select(Norm)
					.Distinct(StringComparer.Ordinal)
					.OrderBy(t => t, StringComparer.Ordinal);

				sb.Append(string.Join(";", canon));
			}

			using var md5 = MD5.Create();
			return md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
		}

		private static string Norm(string s) => (s ?? "").Trim().ToLowerInvariant();

		private static string FloatToString(float v, int decimals)
		{
			// clamp tiny -0.0 to 0
			if (Math.Abs(v) < 1e-8f) v = 0f;
			return Math.Round(v, decimals).ToString($"F{decimals}", CultureInfo.InvariantCulture);
		}

		private static void Normalize(ref Quaternion q)
		{
			float len = (float)Math.Sqrt(q.W * q.W + q.X * q.X + q.Y * q.Y + q.Z * q.Z);
			if (len > 0f)
			{
				float inv = 1f / len;
				q.W *= inv; q.X *= inv; q.Y *= inv; q.Z *= inv;
			}
			else { q.W = 1f; q.X = q.Y = q.Z = 0f; }
		}

		private static void Canonicalize(ref Quaternion q)
		{
			if (q.W < 0f)
			{
				q.W = -q.W; q.X = -q.X; q.Y = -q.Y; q.Z = -q.Z;
			}
		}
	}
}
