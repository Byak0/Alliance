using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;

namespace Alliance.Common.GameModes.Story.Functions
{
	[Serializable]
	[PhrasePreview("{Left} + {Right}")]
	[PhraseTemplate("{Left} plus {Right}")]
	public class AddIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
			=> Resolve(Left, ctx, globals) + Resolve(Right, ctx, globals);

		internal static int Resolve(ValueSource<int> source, TriggerContext ctx, VariableStore globals)
			=> source?.Resolve(ctx, globals) ?? 0;
	}

	[Serializable]
	[PhrasePreview("{Left} - {Right}")]
	[PhraseTemplate("{Left} minus {Right}")]
	public class SubtractIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
			=> AddIntFunction.Resolve(Left, ctx, globals) - AddIntFunction.Resolve(Right, ctx, globals);
	}

	[Serializable]
	[PhrasePreview("min({Left}, {Right})")]
	[PhraseTemplate("minimum of {Left} and {Right}")]
	public class MinIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
			=> Math.Min(AddIntFunction.Resolve(Left, ctx, globals), AddIntFunction.Resolve(Right, ctx, globals));
	}

	[Serializable]
	[PhrasePreview("max({Left}, {Right})")]
	[PhraseTemplate("maximum of {Left} and {Right}")]
	public class MaxIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
			=> Math.Max(AddIntFunction.Resolve(Left, ctx, globals), AddIntFunction.Resolve(Right, ctx, globals));
	}

	[Serializable]
	[PhrasePreview("clamp {Value} to {Min}..{Max}")]
	[PhraseTemplate("clamp {Value} between {Min} and {Max}")]
	public class ClampIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Value")]
		public ValueSource<int> Value = new LiteralValue<int>(0);
		[ConfigProperty(label: "Min")]
		public ValueSource<int> Min = new LiteralValue<int>(0);
		[ConfigProperty(label: "Max")]
		public ValueSource<int> Max = new LiteralValue<int>(1);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			int min = AddIntFunction.Resolve(Min, ctx, globals);
			int max = AddIntFunction.Resolve(Max, ctx, globals);
			if (max < min) (min, max) = (max, min);
			return Math.Min(Math.Max(AddIntFunction.Resolve(Value, ctx, globals), min), max);
		}
	}

	[Serializable]
	[PhrasePreview("{Left} > {Right}")]
	[PhraseTemplate("{Left} is greater than {Right}")]
	public class GreaterThanIntFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
			=> AddIntFunction.Resolve(Left, ctx, globals) > AddIntFunction.Resolve(Right, ctx, globals);
	}
}
