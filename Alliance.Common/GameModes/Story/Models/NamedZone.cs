using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// A reusable zone definition stored on an <see cref="Act"/> and referenced by name from any
	/// condition/action of that act (via <c>NamedZoneFunction</c>). Names should be unique within an act.
	/// </summary>
	[Serializable]
	[PhrasePreview("{Name} - {Zone}")]
	public class NamedZone
	{
		[ConfigProperty(label: "Name", tooltip: "Unique name within this act. Referenced by 'named zone' pickers.")]
		public string Name = "Zone1";

		[ConfigProperty(label: "Enabled", tooltip: "Disabled zones can be ignored by some functions (NearestZoneToAgentFunction) or spawn logic. Can be toggled at runtime with a 'Set zone state' action.")]
		public bool Enabled = true;

		[ConfigProperty(label: "Zone", tooltip: "The reusable zone definition.")]
		public Zone Zone = new Zone();

		public NamedZone() { }

		public NamedZone(string name) { Name = name; }
	}
}
