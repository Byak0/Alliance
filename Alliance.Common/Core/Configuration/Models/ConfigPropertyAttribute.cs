using System;

namespace Alliance.Common.Core.Configuration.Models
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class ConfigPropertyAttribute : Attribute
	{
		public bool IsEditable { get; }
		public string Label { get; }
		public string Tooltip { get; }
		public float MinValue { get; }
		public float MaxValue { get; }
		public string[] PossibleValues => AllianceData.GetData(DataType);
		public AllianceData.DataTypes DataType { get; }

		public ConfigPropertyAttribute(bool isEditable = true, string label = "", string tooltip = null, float minValue = 0, float maxValue = 10, AllianceData.DataTypes dataType = AllianceData.DataTypes.None)
		{
			IsEditable = isEditable;
			Label = label;
			Tooltip = tooltip;
			MinValue = minValue;
			MaxValue = maxValue;
			DataType = dataType;
		}
	}
}
