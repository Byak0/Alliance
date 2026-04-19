using System.Xml.Serialization;

namespace Alliance.Common.Extensions.BuildSystem.Configuration.Models
{
	public class BuildPrefabDefinition
	{
		[XmlAttribute("Id")]
		public string Id { get; set; }

		[XmlAttribute("Module")]
		public string Module { get; set; }

		[XmlAttribute("SourcePath")]
		public string SourcePath { get; set; }
	}
}