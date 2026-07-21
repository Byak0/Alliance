using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story
{
	/// <summary>
	/// Unified expression-tree node for any parameter slot. A slot is always one of:
	/// <list type="bullet">
	/// <item><see cref="LiteralValue{T}"/> — a constant edited directly.</item>
	/// <item><see cref="VariableValue{T}"/> — a reference to a trigger or global variable.</item>
	/// <item><see cref="FunctionCall{T}"/> — a <see cref="Function"/> invocation whose own parameters are
	/// <c>ValueSource&lt;X&gt;</c> slots, giving recursive composability (WC3-style expression trees).</item>
	/// </list>
	/// </summary>
	[Serializable]
	public abstract class ValueSource<T> : IValueSource
	{
		[XmlIgnore]
		public Type ValueType => typeof(T);

		public abstract T Resolve(TriggerContext ctx, VariableStore globals);
	}

	/// <summary>Non-generic metadata surface used by serializer and editor infrastructure.</summary>
	public interface IValueSource
	{
		Type ValueType { get; }
	}

	/// <summary>
	/// Centralizes the concrete expression-node types valid for a closed <see cref="ValueSource{T}"/>.
	/// Runtime engine handles cannot be literals: authors obtain them from variables or functions instead.
	/// </summary>
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

	/// <summary>
	/// A call to a <see cref="Function"/>. The only internal node of the expression tree: recursion comes
	/// from the function's own <c>ValueSource&lt;X&gt;</c> parameters.
	/// </summary>
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
