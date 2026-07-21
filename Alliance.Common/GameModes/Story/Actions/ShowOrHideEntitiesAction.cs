using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Change entity visibility.
	/// </summary>
	[Serializable]
	[PhraseTemplate("{VisibilityType|Show|Hide|Switch visibility of} {ParentEntityOnly|all entities|parent entity and its children,} with tag {Tag} {?VisibilityType==Switch||starting with {DefaultVisibility}}")]
	public class ShowOrHideEntitiesAction : ActionBase
	{
		public enum Visibility
		{
			Show,
			Hide,
			Switch
		}

		[ConfigProperty(label: "Tag", tooltip: "Entities with this tag will be targetted.")]
		public string Tag;
		[ConfigProperty(label: "Visibility", tooltip: "Action to perform on the entities.")]
		public Visibility VisibilityType;
		[ConfigProperty(label: "Default Visibility", tooltip: "First visibility state when using Switch.")]
		public bool DefaultVisibility;
		[ConfigProperty(label: "Restrict to parent entity", tooltip: "If enabled, the action will only check its parent entity and children.")]
		public bool ParentEntityOnly;

		public ShowOrHideEntitiesAction() { }
	}
}