using System;
using System.Globalization;
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
			string category = null)
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

		public bool IsDependencySatisfied(object configInstance)
		{
			if (string.IsNullOrEmpty(Dependency))
				return true;

			string fieldName = Dependency;
			string op = null;
			string compareStr = null;

			if (Dependency[0] == '?')
			{
				int opIdx = -1;
				for (int i = 1; i < Dependency.Length; i++)
				{
					char c = Dependency[i];
					if (c == '=' || c == '!' || c == '>' || c == '<')
					{
						opIdx = i;
						break;
					}
				}

				if (opIdx > 0)
				{
					fieldName = Dependency.Substring(1, opIdx - 1);
					int opLen = (opIdx + 1 < Dependency.Length && Dependency[opIdx + 1] == '=') ? 2 : 1;
					op = Dependency.Substring(opIdx, opLen);
					compareStr = Dependency.Substring(opIdx + opLen);
				}
				else
				{
					fieldName = Dependency.Substring(1);
				}
			}

			FieldInfo dependencyField = configInstance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
			if (dependencyField == null)
				return true;

			if (op == null)
			{
				if (dependencyField.FieldType == typeof(bool))
					return (bool)dependencyField.GetValue(configInstance);
				return true;
			}

			return EvaluateExpression(dependencyField.GetValue(configInstance), op, compareStr);
		}

		public static bool HasDependents(string fieldName, object configInstance)
		{
			if (configInstance == null) return false;

			FieldInfo[] fields = configInstance.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (FieldInfo field in fields)
			{
				ConfigPropertyAttribute attr = field.GetCustomAttribute<ConfigPropertyAttribute>();
				if (attr != null && GetDependencyFieldName(attr.Dependency) == fieldName)
				{
					return true;
				}
			}
			return false;
		}

		private static string GetDependencyFieldName(string dependency)
		{
			if (string.IsNullOrEmpty(dependency))
				return null;

			if (dependency[0] == '?')
			{
				for (int i = 1; i < dependency.Length; i++)
				{
					char c = dependency[i];
					if (c == '=' || c == '!' || c == '>' || c == '<')
						return dependency.Substring(1, i - 1);
				}
				return dependency.Substring(1);
			}

			return dependency;
		}

		private static bool EvaluateExpression(object fieldValue, string op, string compareStr)
		{
			if (fieldValue is bool b)
			{
				bool.TryParse(compareStr, out bool cmp);
				return op switch
				{
					"=" => b == cmp,
					"!=" => b != cmp,
					_ => true
				};
			}

			if (fieldValue is int i)
			{
				if (!int.TryParse(compareStr, out int cmp)) return true;
				return op switch
				{
					"=" => i == cmp,
					"!=" => i != cmp,
					">" => i > cmp,
					"<" => i < cmp,
					">=" => i >= cmp,
					"<=" => i <= cmp,
					_ => true
				};
			}

			if (fieldValue is float f)
			{
				if (!float.TryParse(compareStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float cmp)) return true;
				return op switch
				{
					"=" => Math.Abs(f - cmp) < 0.0001f,
					"!=" => Math.Abs(f - cmp) >= 0.0001f,
					">" => f > cmp,
					"<" => f < cmp,
					">=" => f >= cmp,
					"<=" => f <= cmp,
					_ => true
				};
			}

			string strVal = fieldValue?.ToString() ?? "";
			return op switch
			{
				"=" => string.Equals(strVal, compareStr, StringComparison.OrdinalIgnoreCase),
				"!=" => !string.Equals(strVal, compareStr, StringComparison.OrdinalIgnoreCase),
				_ => true
			};
		}
	}
}
