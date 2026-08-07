using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using static Alliance.Common.GameModes.Story.Utilities.ScenarioData;

namespace Alliance.Common.GameModes.Story.Utilities
{
	public static class ValueSourceHelper
	{
		public enum ValueSourceOrigin
		{
			Literal,
			Variable,
			Function
		}

		public static bool IsValueSourceType(Type type)
		{
			if (type == null) return false;
			if (type == typeof(ValueSource)) return true;
			return type.IsGenericType
				&& type.GetGenericTypeDefinition() == typeof(ValueSource<>);
		}

		public static bool TryGetValueType(Type valueSourceType, out Type valueType)
		{
			if (valueSourceType.IsGenericType && IsValueSourceType(valueSourceType))
			{
				valueType = valueSourceType?.GetGenericArguments()[0];
				return true;
			}

			valueType = null;
			return false;
		}

		public static bool SupportsLiteral(Type valueType)
		{
			return valueType != null
				&& (valueType.IsPrimitive
					|| valueType.IsEnum
					|| valueType == typeof(string)
					|| valueType == typeof(decimal)
					|| valueType == typeof(LocalizedString)
					|| valueType == typeof(Zone)
					|| valueType == typeof(WeakGameEntity)
					|| valueType == typeof(MatrixFrame));
		}

		public static object CreateDefaultLiteralValue(Type valueType)
		{
			if (valueType == typeof(Zone)) return new Zone();
			if (valueType == typeof(LocalizedString)) return new LocalizedString("");
			if (valueType == typeof(string)) return string.Empty;
			return valueType.IsValueType ? Activator.CreateInstance(valueType) : null;
		}

		/// <summary>The concrete literal ValueSource type for a value type, or null if literals aren't supported.
		/// WeakGameEntity and MatrixFrame have dedicated literal types; everything else uses LiteralValue&lt;T&gt;.</summary>
		public static Type GetLiteralType(Type valueType)
		{
			if (!SupportsLiteral(valueType)) return null;
			if (valueType == typeof(WeakGameEntity)) return typeof(SceneEntityLiteralValue);
			if (valueType == typeof(MatrixFrame)) return typeof(FrameLiteralValue);
			return typeof(LiteralValue<>).MakeGenericType(valueType);
		}

		/// <summary>Instantiates the literal ValueSource for a value type, with a sensible default value.</summary>
		public static object CreateLiteralSource(Type valueType)
		{
			if (valueType == typeof(WeakGameEntity)) return new SceneEntityLiteralValue();
			if (valueType == typeof(MatrixFrame)) return new FrameLiteralValue();
			Type literalType = typeof(LiteralValue<>).MakeGenericType(valueType);
			object literal = Activator.CreateInstance(literalType);
			literalType.GetField(nameof(LiteralValue<int>.Value), BindingFlags.Instance | BindingFlags.Public)
				.SetValue(literal, CreateDefaultLiteralValue(valueType));
			return literal;
		}

		public static IReadOnlyList<Type> GetConcreteTypes(Type valueSourceType)
		{
			if (!TryGetValueType(valueSourceType, out Type valueType)) return Array.Empty<Type>();

			List<Type> types = new List<Type>();
			Type literalType = GetLiteralType(valueType);
			if (literalType != null) types.Add(literalType);
			types.Add(typeof(VariableValue<>).MakeGenericType(valueType));
			types.Add(typeof(FunctionCall<>).MakeGenericType(valueType));
			return types;
		}

		public static ValueSourceOrigin? GetCurrentOrigin(object source)
		{
			if (source == null) return null;
			if (source is SceneEntityLiteralValue) return ValueSourceOrigin.Literal;
			if (source is FrameLiteralValue) return ValueSourceOrigin.Literal;

			Type type = source.GetType();
			if (!type.IsGenericType) return null;
			Type definition = type.GetGenericTypeDefinition();
			if (definition == typeof(LiteralValue<>)) return ValueSourceHelper.ValueSourceOrigin.Literal;
			if (definition == typeof(VariableValue<>)) return ValueSourceHelper.ValueSourceOrigin.Variable;
			if (definition == typeof(FunctionCall<>)) return ValueSourceHelper.ValueSourceOrigin.Function;
			return null;
		}

		public static object CreateSource(ValueSourceOrigin origin, Type valueType)
		{
			switch (origin)
			{
				case ValueSourceOrigin.Literal:
					return CreateLiteralSource(valueType);
				case ValueSourceOrigin.Variable:
					return Activator.CreateInstance(typeof(VariableValue<>).MakeGenericType(valueType));
				case ValueSourceOrigin.Function:
					return Activator.CreateInstance(typeof(FunctionCall<>).MakeGenericType(valueType));
				default:
					throw new ArgumentOutOfRangeException(nameof(origin));
			}
		}

		public static Type GetValueSourceTypeToCreate(Type baseType, Scenario scenario, Act act, object localObject = null, object objectToIgnore = null)
		{
			Type typeToCreate = null;

			// Determine the concrete type from the base type, which could be a ValueSource<T> or already a concrete type.
			Type concreteType = baseType.IsGenericType ? baseType.GetGenericArguments()[0] : baseType;

			bool hasVariables = CollectAvailableVariables(concreteType, scenario, act, localObject, objectToIgnore).Length > 0;
			bool hasFunctions = DiscoverConcreteTypes(typeof(Function)).Any(t => FunctionReturns(t, concreteType));

			Type literalType = GetLiteralType(concreteType);
			Type variableType = hasVariables ? typeof(VariableValue<>).MakeGenericType(concreteType) : null;
			Type functionType = hasFunctions ? typeof(FunctionCall<>).MakeGenericType(concreteType) : null;

			typeToCreate = literalType ?? variableType ?? functionType;

			return typeToCreate;
		}

		public static string[] CollectAvailableVariables(Type variableType, Scenario scenario, Act act, object localObject = null, object objectToIgnore = null)
		{
			List<string> collectedVar = new List<string>();

			if (variableType == typeof(Zone))
			{
				CollectZoneNames(collectedVar, act);
				return collectedVar.ToArray();
			}

			CollectLocalVariables(variableType, collectedVar, localObject, objectToIgnore);
			CollectGlobalVariableNames(variableType, collectedVar, scenario);

			return collectedVar.ToArray();
		}

		public static void CollectZoneNames(List<string> collectedVar, Act parentAct)
		{
			if (parentAct?.Zones != null)
			{
				foreach (var zone in parentAct.Zones)
				{
					if (zone == null || string.IsNullOrWhiteSpace(zone.Name)) continue;
					if (!collectedVar.Contains(zone.Name)) collectedVar.Add(zone.Name);
				}
			}
		}

		public static void CollectLocalVariables(Type variableType, List<string> collectedVar, object localObject, object objectToIgnore = null)
		{
			if (localObject == null) return;
			CollectTriggerVariablesFromObject(localObject, variableType, collectedVar, new HashSet<object>());

			// Remove variables produced by the current object itself (or given context) to prevent self-reference.
			if (objectToIgnore != null)
			{
				foreach (FieldInfo f in objectToIgnore.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					VariableOutputAttribute outAttr = f.GetCustomAttribute<VariableOutputAttribute>();
					if (outAttr?.VariableType != null && variableType.IsAssignableFrom(outAttr.VariableType))
					{
						string name = f.GetValue(objectToIgnore) as string;
						if (!string.IsNullOrWhiteSpace(name))
						{
							collectedVar.Remove(name);
						}
					}
				}
			}
		}

		public static void CollectTriggerVariablesFromObject(object obj, Type variableType, List<string> collectedVar, HashSet<object> visited)
		{
			if (obj == null || !visited.Add(obj)) return;

			Type type = obj.GetType();
			if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType) return;

			foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				VariableOutputAttribute outAttr = f.GetCustomAttribute<VariableOutputAttribute>();
				if (outAttr?.VariableType != null && (variableType == null || variableType.IsAssignableFrom(outAttr.VariableType)))
				{
					string name = f.GetValue(obj) as string;
					if (!string.IsNullOrWhiteSpace(name) && !collectedVar.Contains(name))
					{
						collectedVar.Add(name);
					}
				}
			}

			if (obj is IList list)
			{
				foreach (var item in list)
				{
					CollectTriggerVariablesFromObject(item, variableType, collectedVar, visited);
				}
				return;
			}

			foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				Type ft = field.FieldType;
				if (ft.IsPrimitive || ft == typeof(string) || ft.IsEnum || ft.IsValueType) continue;

				object fieldValue = field.GetValue(obj);
				if (fieldValue != null)
				{
					CollectTriggerVariablesFromObject(fieldValue, variableType, collectedVar, visited);
				}
			}
		}

		public static void CollectGlobalVariableNames(Type variableType, List<string> names, Scenario parentScenario)
		{
			if (parentScenario?.Variables == null) return;

			foreach (var scVar in parentScenario.Variables)
			{
				if (scVar == null || string.IsNullOrWhiteSpace(scVar.Name)) continue;
				if (variableType != null && !TypeMatchesFilter(variableType, scVar)) continue;
				if (!names.Contains(scVar.Name)) names.Add(scVar.Name);
			}
		}

		public static Type[] DiscoverConcreteTypes(Type baseType)
		{
			List<Type> types = new List<Type>();
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach (var t in assembly.GetTypes())
					{
						if (baseType.IsAssignableFrom(t) && !t.IsAbstract) types.Add(t);
					}
				}
				catch (ReflectionTypeLoadException ex)
				{
					if (ex.Types != null)
					{
						foreach (var t in ex.Types)
						{
							if (t != null && baseType.IsAssignableFrom(t) && !t.IsAbstract) types.Add(t);
						}
					}
				}
				catch { }
			}
			return types.ToArray();
		}

		public static bool TypeMatchesFilter(Type filterType, ScenarioVariable scVar)
		{
			if (scVar.Type == VariableType.Enum && filterType.IsEnum)
			{
				Type enumType = ScenarioData.AvailableEnumTypes().FirstOrDefault(t => t.Name == scVar.EnumTypeName);
				return enumType != null && filterType == enumType;
			}
			return scVar.Type switch
			{
				VariableType.Int => filterType == typeof(int) || filterType == typeof(float),
				_ => filterType == ScenarioData.GetVariableType(scVar.Type),
			};
		}

		public static bool FunctionReturns(Type functionType, Type resultType)
		{
			if (functionType == null || resultType == null || functionType.IsAbstract) return false;
			try
			{
				if (Activator.CreateInstance(functionType) is Function function)
				{
					return resultType.IsAssignableFrom(function.ReturnType);
				}
			}
			catch
			{
			}
			return false;
		}
	}
}
