using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
			Agent,
			AgentList
		}

		private static List<Type> _availableEnumTypes;
		private static readonly object _cacheLock = new object();

		/// <summary>
		/// Scans all concrete types deriving from Condition, ActionBase, AgentSource, ValueSource&lt;&gt;,
		/// and ZoneSource (when available) to discover enum types used in their public fields.
		/// Results are cached per AppDomain for stable lookups.
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
					typeof(AgentSource)
				};

				List<Type> derivedTypes = GetSerializableDerivedTypes(baseTypes.ToArray());
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

		private static List<Type> GetSerializableDerivedTypes(params Type[] baseTypes)
		{
			IEnumerable<Type> allTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a =>
				{
					try { return a.GetTypes(); }
					catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
					catch { return Enumerable.Empty<Type>(); }
				});

			return allTypes
				.Where(t => t != null && !t.IsAbstract && baseTypes.Any(t.IsSubclassOf))
				.ToList();
		}
	}
}
