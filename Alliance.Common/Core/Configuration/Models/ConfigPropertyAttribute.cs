using System;
using System.Collections.Generic;
using System.Globalization;
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

		// A string representing a dependency condition for this property. The format is:
		// - "{FieldName}" (the property is enabled if the field is true)
		// - "?{FieldName}=Value" (you can use =, !=, >, <, >=, <= for comparison)
		// - "?{FieldName}=Value&?{FieldName2}!=Value2" (you can use multiple conditions separated by &)
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

			// Support compound conditions joined by '&' (all must be satisfied).
			if (Dependency.IndexOf('&') >= 0)
			{
				string[] parts = Dependency.Split('&');
				foreach (string part in parts)
				{
					if (!EvaluateSingleCondition(part.Trim(), configInstance)) return false;
				}
				return true;
			}

			return EvaluateSingleCondition(Dependency, configInstance);
		}

		private bool EvaluateSingleCondition(string dependency, object configInstance)
		{
			if (string.IsNullOrEmpty(dependency))
				return true;

			string fieldName = dependency;
			string op = null;
			string compareStr = null;

			if (dependency[0] == '?')
			{
				int opIdx = -1;
				for (int i = 1; i < dependency.Length; i++)
				{
					char c = dependency[i];
					if (c == '=' || c == '!' || c == '>' || c == '<')
					{
						opIdx = i;
						break;
					}
				}

				if (opIdx > 0)
				{
					fieldName = dependency.Substring(1, opIdx - 1);
					int opLen = (opIdx + 1 < dependency.Length && dependency[opIdx + 1] == '=') ? 2 : 1;
					op = dependency.Substring(opIdx, opLen);
					compareStr = dependency.Substring(opIdx + opLen);
				}
				else
				{
					fieldName = dependency.Substring(1);
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
				if (attr != null && GetDependencyFieldNames(attr.Dependency).Contains(fieldName))
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>All field names referenced by a dependency expression (compound expressions joined
		/// by '&' reference several) - used to know which changes must refresh dependents.</summary>
		public static IEnumerable<string> GetDependencyFieldNames(string dependency)
		{
			if (string.IsNullOrEmpty(dependency)) yield break;
			foreach (string part in dependency.Split('&'))
			{
				string condition = part.Trim();
				if (string.IsNullOrEmpty(condition)) continue;

				if (condition[0] == '?')
				{
					for (int i = 1; i < condition.Length; i++)
					{
						char c = condition[i];
						if (c == '=' || c == '!' || c == '>' || c == '<')
						{
							yield return condition.Substring(1, i - 1);
							break;
						}
					}
				}
				else
				{
					yield return condition;
				}
			}
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
