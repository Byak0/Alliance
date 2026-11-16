using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhrasePreview("{VisibilityType|Show|Hide|Switch} {ParentEntityOnly|all entities|parent entity} tagged {Tag}")]
	[PhraseTemplate("{VisibilityType|Show|Hide|Switch} {ParentEntityOnly|all entities|parent entity and children} tagged {Tag}{?VisibilityType==Switch:, starting with {DefaultVisibility}}")]
	public class ShowOrHideEntitiesAction : ActionBase
	{
		public enum Visibility
		{
			Show,
			Hide,
			Switch
		}

		[ConfigProperty(label: "Tag", tooltip: "Entities with this tag will be targeted.")]
		public ValueSource<string> Tag = new LiteralValue<string>("");
		[ConfigProperty(label: "Visibility", tooltip: "Action to perform on the entities.")]
		public Visibility VisibilityType;
		[ConfigProperty(label: "Default Visibility", tooltip: "First visibility state when using Switch.")]
		public ValueSource<bool> DefaultVisibility = new LiteralValue<bool>(true);
		[ConfigProperty(label: "Restrict to parent entity", tooltip: "If enabled, the action will only check its parent entity and children.")]
		public bool ParentEntityOnly;

		public ShowOrHideEntitiesAction() { }
	}
}