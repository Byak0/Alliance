using System.Collections.Generic;
using System.Xml.Serialization;

namespace Alliance.Common.Extensions.BuildSystem.Configuration.Models
{
	[XmlRoot("BuildPrefabPreset")]
	public class BuildPrefabPreset
	{
		[XmlElement("Name")]
		public string Name { get; set; } = "Default";

		[XmlArray("AllowedPrefabs")]
		[XmlArrayItem("Prefab")]
		public List<BuildPrefabReference> AllowedPrefabs { get; set; } = new List<BuildPrefabReference>();
	}
}