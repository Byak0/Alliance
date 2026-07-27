using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Utilities
{
	public static class ScenarioData
	{
		public enum VariableType
		{
			Int,
			Float,
			Bool,
			String,
			Enum,
			Agent
		}

		private static List<Type> _availableEnumTypes;
		private static readonly object _cacheLock = new object();

		public static Type GetVariableType(VariableType variableType)
		{
			return variableType switch
			{
				VariableType.Int => typeof(int),
				VariableType.Float => typeof(float),
				VariableType.Bool => typeof(bool),
				VariableType.String => typeof(string),
				VariableType.Enum => typeof(Enum),
				VariableType.Agent => typeof(Agent),
				_ => throw new ArgumentOutOfRangeException(nameof(variableType), variableType, null)
			};
		}

		/// <summary>
		/// Scans all enum types used in Condition, ActionBase, and Function
		/// </summary>
		public static List<Type> AvailableEnumTypes()
		{
			if (_availableEnumTypes != null) return _availableEnumTypes;

			lock (_cacheLock)
			{
				if (_availableEnumTypes != null) return _availableEnumTypes;

				HashSet<Type> enumTypes = new HashSet<Type>();

				List<Type> baseTypes = new List<Type>
				{
					typeof(Condition),
					typeof(ActionBase),
					typeof(Function)
				};

				List<Type> derivedTypes = SerializeHelper.GetSerializableDerivedTypes(baseTypes.ToArray()).ToList();
				// also scan ValueSource<> types used as fields in those derived types
				foreach (Type t in derivedTypes)
				{
					CollectEnumTypesFromFields(t, enumTypes);
				}

				// Also scan the base types themselves
				foreach (Type t in baseTypes)
				{
					CollectEnumTypesFromFields(t, enumTypes);
				}

				_availableEnumTypes = enumTypes.OrderBy(t => t.Name).ToList();
				_availableEnumTypes.Remove(typeof(VariableType));
				return _availableEnumTypes;
			}
		}

		private static void CollectEnumTypesFromFields(Type type, HashSet<Type> enumTypes)
		{
			foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				if (field.FieldType.IsEnum)
				{
					enumTypes.Add(field.FieldType);
				}
				// Also look into generic arguments of list/collection fields
				if (field.FieldType.IsGenericType)
				{
					foreach (Type arg in field.FieldType.GetGenericArguments())
					{
						if (arg.IsEnum)
						{
							enumTypes.Add(arg);
						}
					}
				}
			}
		}
	}
}
