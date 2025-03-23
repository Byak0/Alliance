using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Change entity visibility.
	/// </summary>
	[Serializable]
	public class ShowOrHideEntitiesAction : ActionBase
	{
		[ScenarioEditor(label: "Tag", tooltip: "Entities with this tag will be targetted.")]
		public string Tag;
		[ScenarioEditor(label: "Visible", tooltip: "Show or hide the entities.")]
		public bool Visible;
		[ScenarioEditor(label: "Toggle", tooltip: "If enabled, the action will toggle the visibility of the entities instead of setting it to the specified value.")]
		public bool Toggle;
		[ScenarioEditor(label: "Restrict to parent entity", tooltip: "If enabled, the action will only check its parent entity and children.")]
		public bool ParentEntityOnly;

		public ShowOrHideEntitiesAction() { }
	}
}