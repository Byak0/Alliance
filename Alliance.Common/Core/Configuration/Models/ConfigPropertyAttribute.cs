using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
		public string Dependency { get; }
		public string Category { get; }

		public ConfigPropertyAttribute(
			bool isEditable = true, 
			string label = "", 
			string tooltip = null, 
			float minValue = 0, 
			float maxValue = 10, 
			AllianceData.DataTypes dataType = AllianceData.DataTypes.None, 
			string dependency = null,
			string category = "General")
		{
			IsEditable = isEditable;
			Label = label;
			Tooltip = tooltip;
			MinValue = minValue;
			MaxValue = maxValue;
			DataType = dataType;
			Dependency = dependency;
			Category = category;
		}

		/// <summary>
		/// Check if the dependency is satisfied for this property.
		/// Returns true if no dependency is set or if the dependency field is true.
		/// </summary>
		public bool IsDependencySatisfied(object configInstance)
		{
			if (string.IsNullOrEmpty(Dependency))
				return true;

			FieldInfo dependencyField = configInstance.GetType().GetField(Dependency, BindingFlags.Public | BindingFlags.Instance);
			
			if (dependencyField == null)
			{
				// Dependency field not found, log warning and assume satisfied
				return true;
			}

			// Only boolean dependencies are supported
			if (dependencyField.FieldType == typeof(bool))
			{
				return (bool)dependencyField.GetValue(configInstance);
			}

			return true;
		}

		/// <summary>
		/// Check if any field depends on the given field name
		/// </summary>
		public static bool HasDependents(string fieldName, object configInstance)
		{
			if (configInstance == null) return false;

			FieldInfo[] fields = configInstance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (FieldInfo field in fields)
			{
				ConfigPropertyAttribute attr = field.GetCustomAttribute<ConfigPropertyAttribute>();
				if (attr != null && attr.Dependency == fieldName)
				{
					return true;
				}
			}
			return false;
		}
	}
}
