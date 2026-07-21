using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Xml.Serialization;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// A positioned region of space, composed of three independent axes:
	/// <list type="bullet">
	/// <item><see cref="Anchor"/>: what <see cref="Position"/> is relative to (world, host entity, named entity, agent).</item>
	/// <item><see cref="Shape"/>: the geometry (circle today, box/polygon tomorrow).</item>
	/// <item><see cref="Position"/>: the authored coordinates, interpreted through the anchor.</item>
	/// </list>
	/// </summary>
	[Serializable]
	[PhrasePreview("{Shape} {Anchor}")]
	public class Zone : ISerializationCallback
	{
		[ConfigProperty(label: "Anchor", tooltip: "What the position is relative to: the scene (absolute), the host entity, a named entity, or an agent variable.")]
		public ZoneAnchor Anchor = new WorldAnchor();

		[ConfigProperty(label: "Shape", tooltip: "Geometry of the zone. Circle is the default.")]
		public ZoneShape Shape = new CircleShape();

		[ConfigProperty(isEditable: false)]
		public float X, Y, Z;

		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		public Vec3 Position;

		/// <summary>Entity hosting the enclosing ScriptedEvent (the map entity of an AL_TriggerAction).</summary>
		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		public WeakGameEntity HostEntity = WeakGameEntity.Invalid;

		/// <summary>Convenience: bounding radius (prefilter + debug rendering), delegating to the shape.</summary>
		public float Radius => Shape?.BoundingRadius ?? 0f;

		/// <summary>World-space center of the zone, resolved through its anchor.</summary>
		public Vec3 ResolveCenter(TriggerContext ctx = null, VariableStore globals = null)
			=> Anchor != null ? Anchor.Resolve(Position, HostEntity, ctx, globals) : Position;

		/// <summary>Precise containment test in world space (delegates to the shape).</summary>
		public bool Contains(Vec3 worldPoint, TriggerContext ctx = null, VariableStore globals = null)
		{
			if (Shape == null) return false;
			return Shape.Contains(ResolveCenter(ctx, globals), worldPoint);
		}

		/// <summary>Stores the host entity and lets the anchor resolve/cache its target (e.g. named entity).</summary>
		public void Register(WeakGameEntity hostEntity)
		{
			HostEntity = hostEntity;
			Anchor?.OnRegister(hostEntity);
		}

		/// <summary>Converts a world-space point back into the zone's local frame (for click placement).</summary>
		public void SetPositionFromWorld(Vec3 worldPoint)
			=> Position = Anchor != null ? Anchor.WorldToLocal(worldPoint, HostEntity) : worldPoint;

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
