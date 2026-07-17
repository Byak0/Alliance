using Alliance.Common.Core.Configuration.Models;
using System;
using System.Xml.Serialization;

namespace Alliance.Common.GameModes.Story
{
	[Serializable]
	[XmlInclude(typeof(LiteralValue<>))]
	[XmlInclude(typeof(VariableValue<>))]
	public abstract class ValueSource<T>
	{
		public abstract T Resolve(TriggerContext ctx, VariableStore globals);
	}

	[Serializable]
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
}
