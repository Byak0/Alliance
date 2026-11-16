using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Non-generic base class for <see cref="ValueSource{T}"/>. Used for editor.
	/// </summary>
	public abstract class ValueSource
	{
		public abstract Type ValueType { get; }
		public object ResolveObject(VariableStore context = null) => ResolveObject(context, ScenarioManager.Instance.Globals);
		public abstract object ResolveObject(VariableStore context, VariableStore globals);
	}

	/// <summary>
	/// Unified expression-tree node for any parameter slot. A slot is always one of:
	/// <list type="bullet">
	/// <item><see cref="LiteralValue{T}"/> — a constant edited directly.</item>
	/// <item><see cref="VariableValue{T}"/> — a reference to a local or global variable.</item>
	/// <item><see cref="FunctionCall{T}"/> — a <see cref="Function"/> invocation whose own parameters are
	/// <c>ValueSource&lt;X&gt;</c> slots, giving recursive composability (WC3-style expression trees).</item>
	/// </list>
	/// </summary>
	[Serializable]
	public abstract class ValueSource<T> : ValueSource
	{
		[XmlIgnore]
		public override Type ValueType => typeof(T);

		public override object ResolveObject(VariableStore context, VariableStore globals) => Resolve(context, globals);

		public virtual T Resolve(VariableStore context = null) => Resolve(context, ScenarioManager.Instance.Globals);

		public abstract T Resolve(VariableStore context, VariableStore globals);
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

		public override T Resolve(VariableStore context, VariableStore globals) => Value;
	}

	[Serializable]
	[PhrasePreview("{VariableName}")]
	[PhraseTemplate("{VariableName}")]
	public class VariableValue<T> : ValueSource<T>
	{
		[ConfigProperty(label: "Variable", tooltip: "Local or global variable holding the value.")]
		public string VariableName;

		public VariableValue() { }

		public override T Resolve(VariableStore context, VariableStore globals)
		{
			if(String.IsNullOrEmpty(VariableName)) return default;
			if (context != null && context.Has(VariableName)) return context.Get<T>(VariableName);
			if (globals != null && globals.Has(VariableName)) return globals.Get<T>(VariableName);
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

		public override T Resolve(VariableStore context, VariableStore globals)
		{
			if (Function == null) return default;
			object result = Function.Evaluate(context);
			return result is T typed ? typed : default;
		}
	}
}
