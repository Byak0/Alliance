using Alliance.Common.GameModes.Story.Utilities;
using System.Xml.Serialization;
using TaleWorlds.Engine;
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
		public bool UseLocalSpace = false;

		[ScenarioEditor(isEditable: false)]
		[XmlIgnore]
		public GameEntity LocalEntity;

		[XmlIgnore]
		public Vec3 Position;

		public Vec3 GlobalPosition
		{
			get
			{
				if (UseLocalSpace && LocalEntity != null)
				{
					return LocalEntity.GlobalPosition + Position;
				}
				return Position;
			}
		}

		public SerializableZone(Vec3 position, float radius)
		{
			Position = position;
			X = position.x;
			Y = position.y;
			Z = position.z;
			Radius = radius;
		}

		public SerializableZone() { }

		public void Register(GameEntity localEntity)
		{
			if (UseLocalSpace)
			{
				LocalEntity = localEntity;
			}
		}

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
