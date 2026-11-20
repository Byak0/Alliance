using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Functions
{
	[Serializable]
	[PhrasePreview("not {Value}")]
	[PhraseTemplate("not {Value}")]
	public class NotFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Value")]
		public ValueSource<bool> Value = new LiteralValue<bool>(false);

		public override object Evaluate(VariableStore context)
			=> !(Value?.Resolve(context) ?? false);
	}

	[Serializable]
	[PhrasePreview("{Left} and {Right}")]
	[PhraseTemplate("{Left} and {Right}")]
	public class AndFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<bool> Left = new LiteralValue<bool>(false);
		[ConfigProperty(label: "Right")]
		public ValueSource<bool> Right = new LiteralValue<bool>(false);

		public override object Evaluate(VariableStore context)
			=> (Left?.Resolve(context) ?? false) && (Right?.Resolve(context) ?? false);
	}

	[Serializable]
	[PhrasePreview("{Left} or {Right}")]
	[PhraseTemplate("{Left} or {Right}")]
	public class OrFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<bool> Left = new LiteralValue<bool>(false);
		[ConfigProperty(label: "Right")]
		public ValueSource<bool> Right = new LiteralValue<bool>(false);

		public override object Evaluate(VariableStore context)
			=> (Left?.Resolve(context) ?? false) || (Right?.Resolve(context) ?? false);
	}
}
