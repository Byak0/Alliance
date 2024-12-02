using Alliance.Common.GameModes.Story.Utilities;
using System.Xml.Serialization;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Serializable zone storing a Vec3 and a radius.
	/// </summary>
	public class SerializableZone : ISerializationCallback
	{
		public float X;
		public float Y;
		public float Z;
		public float Radius = 1f;

		[XmlIgnore]
		public Vec3 Position;

		public SerializableZone(Vec3 position, float radius)
		{
			Position = position;
			X = position.x;
			Y = position.y;
			Z = position.z;
			Radius = radius;
		}

		public SerializableZone() { }

		public void OnBeforeSerialize()
		{
			X = Position.x;
			Y = Position.y;
			Z = Position.z;
		}

		public void OnAfterDeserialize()
		{
			Position = new Vec3(X, Y, Z);
		}
	}
}
