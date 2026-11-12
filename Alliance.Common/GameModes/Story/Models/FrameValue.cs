using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// A serializable <see cref="MatrixFrame"/> (position + rotation + scale) using the native scene
	/// convention: rotation is Euler in <b>radians</b> (Vec3 = pitch/roll/yaw applied via
	/// <see cref="Mat3.ApplyEulerAngles"/>), scale is per-axis (applied via <see cref="Mat3.ApplyScaleLocal"/>).
	/// Stored as nine editable floats so the editor renders one numeric field per component.
	/// </summary>
	[Serializable]
	[PhrasePreview("{Px},{Py},{Pz}")]
	public class FrameValue
	{
		[ConfigProperty(label: "X", category: "Position")]
		public float Px;
		[ConfigProperty(label: "Y", category: "Position")]
		public float Py;
		[ConfigProperty(label: "Z", category: "Position")]
		public float Pz;

		[ConfigProperty(label: "X (Pitch)", tooltip: "Radians.", category: "Rotation")]
		public float Rx;
		[ConfigProperty(label: "Y (Roll)", tooltip: "Radians.", category: "Rotation")]
		public float Ry;
		[ConfigProperty(label: "Z (Yaw)", tooltip: "Radians.", category: "Rotation")]
		public float Rz;

		[ConfigProperty(label: "X", category: "Scale")]
		public float Sx = 1f;
		[ConfigProperty(label: "Y", category: "Scale")]
		public float Sy = 1f;
		[ConfigProperty(label: "Z", category: "Scale")]
		public float Sz = 1f;

		public FrameValue() { }

		/// <summary>Composes the native <see cref="MatrixFrame"/> from the nine components.</summary>
		public MatrixFrame ToFrame()
		{
			MatrixFrame frame = MatrixFrame.Identity;
			frame.origin = new Vec3(Px, Py, Pz);
			frame.rotation.ApplyEulerAngles(new Vec3(Rx, Ry, Rz));
			Vec3 scale = new Vec3(Sx, Sy, Sz);
			if (scale.x != 1f || scale.y != 1f || scale.z != 1f)
			{
				frame.rotation.ApplyScaleLocal(scale);
			}
			return frame;
		}

		/// <summary>Extracts a <see cref="FrameValue"/> from a live frame (used by the ghost editor on confirm).</summary>
		public static FrameValue FromFrame(MatrixFrame frame)
		{
			Vec3 euler = frame.rotation.GetEulerAngles();
			Vec3 scale = frame.rotation.GetScaleVector();
			return new FrameValue
			{
				Px = frame.origin.x, Py = frame.origin.y, Pz = frame.origin.z,
				Rx = euler.x, Ry = euler.y, Rz = euler.z,
				Sx = scale.x, Sy = scale.y, Sz = scale.z
			};
		}

		/// <summary>Copies all nine components from another <see cref="FrameValue"/>.</summary>
		public void CopyFrom(FrameValue other)
		{
			if (other == null) return;
			Px = other.Px; Py = other.Py; Pz = other.Pz;
			Rx = other.Rx; Ry = other.Ry; Rz = other.Rz;
			Sx = other.Sx; Sy = other.Sy; Sz = other.Sz;
		}
	}
}
