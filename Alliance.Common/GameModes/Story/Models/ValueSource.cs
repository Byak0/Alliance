using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story.Models
{
	[Serializable]
	public abstract class ValueSource<T> : IValueSource
	{
		[XmlIgnore]
		public Type ValueType => typeof(T);

		public abstract T Resolve(TriggerContext ctx, VariableStore globals);
	}

	public interface IValueSource
	{
		Type ValueType { get; }
	}

	public static class ValueSourceTypeSupport
	{
		public static bool IsValueSourceType(Type type)
		{
			return type != null
				&& type.IsGenericType
				&& type.GetGenericTypeDefinition() == typeof(ValueSource<>);
		}

		public static bool TryGetValueType(Type valueSourceType, out Type valueType)
		{
			if (IsValueSourceType(valueSourceType))
			{
				valueType = valueSourceType.GetGenericArguments()[0];
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
					|| valueType == typeof(Zone));
		}

		public static IReadOnlyList<Type> GetConcreteTypes(Type valueSourceType)
		{
			if (!TryGetValueType(valueSourceType, out Type valueType)) return Array.Empty<Type>();

			List<Type> types = new List<Type>();
			if (SupportsLiteral(valueType)) types.Add(typeof(LiteralValue<>).MakeGenericType(valueType));
			types.Add(typeof(VariableValue<>).MakeGenericType(valueType));
			types.Add(typeof(FunctionCall<>).MakeGenericType(valueType));
			return types;
		}
	}

	[Serializable]
	[PhrasePreview("{Value}")]
	[PhraseTemplate("{Value}")]
	public class LiteralValue<T> : ValueSource<T>
	{
		[ConfigProperty]
		public T Value;

		public LiteralValue() { }

		public LiteralValue(T value) { Value = value; }

		public override T Resolve(TriggerContext ctx, VariableStore globals) => Value;
	}

	[Serializable]
	[PhrasePreview("{VariableName}")]
	[PhraseTemplate("{VariableName}")]
	public class VariableValue<T> : ValueSource<T>
	{
		[ConfigProperty(label: "Variable", tooltip: "Trigger or global variable holding the value.")]
		public string VariableName = "";

		public VariableValue() { }

		public override T Resolve(TriggerContext ctx, VariableStore globals)
		{
			if (ctx != null && ctx.Has(VariableName))
				return ctx.Get<T>(VariableName);
			if (globals != null && globals.Has(VariableName))
				return globals.Get<T>(VariableName);
			return default;
		}
	}

	[Serializable]
	[PhrasePreview("{Function}")]
	public class FunctionCall<T> : ValueSource<T>
	{
		[ConfigProperty(label: "Function", tooltip: "Function to evaluate. Its parameters are themselves value slots.")]
		public Function Function;

		public FunctionCall() { }

		public FunctionCall(Function function) { Function = function; }

		public override T Resolve(TriggerContext ctx, VariableStore globals)
		{
			if (Function == null) return default;
			object result = Function.Evaluate(ctx, globals);
			return result is T typed ? typed : default;
		}
	}
}
