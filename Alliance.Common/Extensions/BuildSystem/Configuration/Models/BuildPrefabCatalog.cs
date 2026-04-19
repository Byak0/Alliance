using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Alliance.Common.Extensions.BuildSystem.Configuration.Models
{
	[XmlRoot("BuildPrefabCatalog")]
	public class BuildPrefabCatalog
	{
		[XmlElement("GeneratedAtUtc")]
		public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

		[XmlArray("Prefabs")]
		[XmlArrayItem("Prefab")]
		public List<BuildPrefabDefinition> Prefabs { get; set; } = new List<BuildPrefabDefinition>();
	}
}