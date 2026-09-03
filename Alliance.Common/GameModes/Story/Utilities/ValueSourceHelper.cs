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

		/// <summary>Ids of the cinematics defined on the scenario (with an empty "none" entry first),
		/// feeding the [CinematicRef] dropdowns in the editor.</summary>
		public static string[] CollectAvailableCinematics(Scenario scenario)
		{
			List<string> ids = new List<string> { "" };
			if (scenario?.Cinematics != null)
			{
				foreach (var cinematic in scenario.Cinematics)
				{
					if (cinematic != null && !string.IsNullOrEmpty(cinematic.Name)) ids.Add(cinematic.Name);
				}
			}
			return ids.ToArray();
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

		/// <summary>One dynamic slot of an object graph: a non-literal ValueSource (Variable or
		/// FunctionCall) living in a field or a list item, that callers can read or rewrite.</summary>
		public readonly struct DynamicSlot
		{
			public readonly object Owner;
			public readonly FieldInfo Field;
			/// <summary>For list-item slots: the list and index (Field is null).</summary>
			public readonly IList List;
			public readonly int ListIndex;
			public DynamicSlot(object owner, FieldInfo field) { Owner = owner; Field = field; List = null; ListIndex = -1; }
			public DynamicSlot(IList list, int index) { Owner = null; Field = null; List = list; ListIndex = index; }
		}

		/// <summary>Collects the [SyncToClient]-marked ValueSource fields (and lists of them) of the
		/// object graph, in a deterministic order. The server resolves these slots in order, ships the
		/// bare values, and the client rewrites slot N with value N as a Literal. Slot internals are not
		/// walked: the server resolves whole expressions, the client replaces them.</summary>
		public static List<DynamicSlot> CollectDynamicSlots(object root)
		{
			List<DynamicSlot> slots = new List<DynamicSlot>();
			CollectDynamicSlots(root, slots, new HashSet<object>());
			return slots;
		}

		/// <summary>Load-time warm-up: populates the per-type field cache so the first play/execution
		/// pays no reflection cost.</summary>
		public static void PrewarmDynamicSlots(object root) => CollectDynamicSlots(root);

		/// <summary>Per-type public instance fields, sorted by name so the walk order is deterministic.</summary>
		private static readonly Dictionary<Type, FieldInfo[]> FieldsCache = new Dictionary<Type, FieldInfo[]>();

		private static FieldInfo[] GetFieldsCached(Type type)
		{
			if (!FieldsCache.TryGetValue(type, out FieldInfo[] fields))
			{
				fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
				System.Array.Sort(fields, (a, b) => string.CompareOrdinal(a.Name, b.Name));
				FieldsCache[type] = fields;
			}
			return fields;
		}

		private static void CollectDynamicSlots(object obj, List<DynamicSlot> slots, HashSet<object> visited)
		{
			if (obj == null || !visited.Add(obj)) return;

			Type type = obj.GetType();
			if (type.IsPrimitive || type == typeof(string) || type.IsEnum || type.IsValueType) return;
			if (!type.IsSerializable && !type.IsGenericType && !typeof(ValueSource).IsAssignableFrom(type)) return;

			if (obj is IList list)
			{
				foreach (object item in list) CollectDynamicSlots(item, slots, visited);
				return;
			}

			foreach (FieldInfo field in GetFieldsCached(type))
			{
				Type ft = field.FieldType;
				if (ft.IsPrimitive || ft == typeof(string) || ft.IsEnum || ft.IsValueType) continue;

				// Collection is opt-in ([SyncToClient]); traversal always descends so marked slots
				// nested anywhere in the graph are found.
				if (field.GetCustomAttribute<Attributes.SyncToClientAttribute>() != null)
				{
					if (typeof(ValueSource).IsAssignableFrom(ft))
					{
						object value = field.GetValue(obj);
						// Literal slots need no sync; null slots neither. Both sides classify identically
						// from the shared data, so the walk order matches.
						if (value == null) continue;
						Type runtimeType = value.GetType();
						if (runtimeType.IsGenericType && runtimeType.GetGenericTypeDefinition() == typeof(LiteralValue<>)) continue;
						slots.Add(new DynamicSlot(obj, field));
						continue;
					}

					if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>)
						&& typeof(ValueSource).IsAssignableFrom(ft.GetGenericArguments()[0]))
					{
						IList slotList = (IList)field.GetValue(obj);
						if (slotList == null) continue;
						for (int i = 0; i < slotList.Count; i++)
						{
							object item = slotList[i];
							if (item == null) continue;
							Type itemType = item.GetType();
							if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(LiteralValue<>)) continue;
							slots.Add(new DynamicSlot(slotList, i));
						}
						continue;
					}
				}

				object fieldValue = field.GetValue(obj);
				if (fieldValue != null) CollectDynamicSlots(fieldValue, slots, visited);
			}
		}

		/// <summary>The ValueSource held by a slot (field or list item), or null.</summary>
		public static ValueSource GetSlotValueSource(in DynamicSlot slot)
		{
			return slot.Field != null ? (ValueSource)slot.Field.GetValue(slot.Owner) : (ValueSource)slot.List[slot.ListIndex];
		}

		/// <summary>Rewrites a slot (field or list item) with a received value as a Literal.</summary>
		public static bool SetSlotLiteral(in DynamicSlot slot, object value)
		{
			if (slot.Field != null) return SetSlotLiteral(slot.Owner, slot.Field, value);
			return SetSlotLiteral(slot.List, slot.ListIndex, value);
		}

		/// <summary>Writes the value into a ValueSource field as a Literal, creating it when needed.
		/// Shared by the cinematic dynamic-data path and ActionBase's [SyncToClient] injection.</summary>
		public static bool SetSlotLiteral(object owner, FieldInfo field, object value)
		{
			Type fieldType = field.FieldType;
			if (!fieldType.IsGenericType) return false;
			object literal = BuildLiteral(fieldType.GetGenericArguments()[0], value);
			field.SetValue(owner, literal);
			return true;
		}

		/// <summary>Replaces a list item with a Literal holding the value.</summary>
		public static bool SetSlotLiteral(IList list, int index, object value)
		{
			object item = list[index];
			Type itemType = item?.GetType();
			if (itemType == null || !itemType.IsGenericType) return false;
			list[index] = BuildLiteral(itemType.GetGenericArguments()[0], value);
			return true;
		}

		private static object BuildLiteral(Type valueType, object value)
		{
			Type literalType = typeof(LiteralValue<>).MakeGenericType(valueType);
			object literal = Activator.CreateInstance(literalType);
			literalType.GetField(nameof(LiteralValue<int>.Value)).SetValue(literal, value);
			return literal;
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
