using System.Xml.Serialization;

namespace Alliance.Common.Extensions.BuildSystem.Configuration.Models
{
	public class BuildPrefabReference
	{
		[XmlAttribute("Id")]
		public string Id { get; set; }
	}
}