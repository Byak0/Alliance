using System;

namespace Alliance.Common.Core.Configuration.Models
{
	[AttributeUsage(AttributeTargets.Field)]
	public class ConfigPropertyAttribute : Attribute
	{
		public string Name { get; set; }
		public string Description { get; set; }
		public float MinValue { get; set; }
		public float MaxValue { get; set; }
		public readonly ConfigValueType ValueType;

		public ConfigPropertyAttribute(string name, string description, ConfigValueType valueType, float minValue = 0, float maxValue = 10)
		{
			Name = name;
			Description = description;
			MinValue = minValue;
			MaxValue = maxValue;
			ValueType = valueType;
		}
	}

	public enum ConfigValueType
	{
		Bool,
		Integer,
		Float,
		Enum
	}
}
