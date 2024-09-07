using System;

namespace Alliance.Common.GameModes.Story.Utilities
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ScenarioEditorAttribute : Attribute
	{
		public bool IsEditable { get; }
		public string Label { get; }
		public string Tooltip { get; }
		public string[] PossibleValues => ScenarioData.GetData(DataType);
		public ScenarioData.DataTypes DataType { get; }

		public ScenarioEditorAttribute(bool isEditable = true, string label = null, string tooltip = null, ScenarioData.DataTypes dataType = ScenarioData.DataTypes.None)
		{
			IsEditable = isEditable;
			Label = label;
			Tooltip = tooltip;
			DataType = dataType;
		}
	}
}
