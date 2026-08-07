using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Serializable descriptor identifying a scene <see cref="TaleWorlds.Engine.GameEntity"/> across
	/// save/load and at runtime. Holds the <c>RefId</c> of an <c>AL_EntityMarker</c> script attached to
	/// the entity (a stable GUID), so the reference survives rename/duplication of the entity.
	/// Resolved to a live <see cref="TaleWorlds.Engine.WeakGameEntity"/> at runtime through
	/// <see cref="Alliance.Common.Extensions.BuildSystem.EntityMarkerIndex"/>.
	/// </summary>
	[Serializable]
	[PhrasePreview("{DisplayName}")]
	public class GameEntityRef
	{
		[ConfigProperty(label: "Ref Id", tooltip: "GUID of the entity's AL_EntityMarker script.")]
		public string RefId = "";

		// Human-readable hint captured at pick time (entity name); debug/validation only.
		[ConfigProperty(isEditable: false)]
		public string DisplayName = "";

		public GameEntityRef() { }

		public GameEntityRef(string refId, string displayName = "")
		{
			RefId = refId;
			DisplayName = displayName;
		}
	}
}
