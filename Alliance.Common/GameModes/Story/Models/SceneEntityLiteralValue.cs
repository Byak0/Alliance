using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// The "literal" origin for a <c>ValueSource&lt;WeakGameEntity&gt;</c> slot. Holds a serializable
	/// <see cref="GameEntityRef"/> (a GUID) that is resolved to a live <see cref="WeakGameEntity"/> at
	/// runtime through <see cref="EntityMarkerIndex"/>. This is the entity equivalent of
	/// <c>LiteralValue&lt;Zone&gt;</c> paired with the "Select entity on map" editor flow.
	/// </summary>
	[Serializable]
	[PhrasePreview("entity {Ref}")]
	[PhraseTemplate("scene entity {Ref}")]
	public class SceneEntityLiteralValue : ValueSource<WeakGameEntity>
	{
		[ConfigProperty(label: "Entity", tooltip: "A scene entity picked visually.")]
		public GameEntityRef Ref = new GameEntityRef();

		public SceneEntityLiteralValue() { }

		public SceneEntityLiteralValue(GameEntityRef gameEntityRef) { Ref = gameEntityRef; }

		public override WeakGameEntity Resolve(VariableStore context, VariableStore globals)
			=> EntityMarkerIndex.Resolve(Ref?.RefId);
	}
}
