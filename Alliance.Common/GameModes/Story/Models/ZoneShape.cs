using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;
using System.Xml.Serialization;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Geometry of a <see cref="Zone"/>. Adding a new shape = one subclass + its [XmlInclude] on this base.
	/// The zone's position/anchor are handled by <see cref="Zone"/>/<see cref="ZoneAnchor"/>; a shape only
	/// deals with "given a center and a size, is this point inside?" and exposes a bounding radius for the
	/// broad-phase spatial query.
	/// </summary>
	[Serializable]
	[XmlInclude(typeof(CircleShape))]
	public abstract class ZoneShape
	{
		/// <summary>Bounding radius used for the broad-phase agent query and debug rendering.</summary>
		public abstract float BoundingRadius { get; }

		/// <summary>Precise containment test, expressed in world space (center already resolved).</summary>
		public abstract bool Contains(Vec3 center, Vec3 worldPoint);
	}

	/// <summary>Spherical zone defined by a radius (default shape, equivalent to the former model).</summary>
	[Serializable]
	[PhrasePreview("circle (r:{Radius})")]
	public class CircleShape : ZoneShape
	{
		[ConfigProperty(label: "Radius", tooltip: "Radius of the circular zone.", minValue: 0f, maxValue: 1000f)]
		public float Radius = 1f;

		public override float BoundingRadius => Radius;

		public override bool Contains(Vec3 center, Vec3 worldPoint) => worldPoint.Distance(center) <= Radius;
	}
}
