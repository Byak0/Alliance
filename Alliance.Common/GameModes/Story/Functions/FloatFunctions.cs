using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;

namespace Alliance.Common.GameModes.Story.Functions
{
	[Serializable]
	[PhrasePreview("{Left} + {Right}")]
	[PhraseTemplate("{Left} plus {Right}")]
	public class AddFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(0f);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
			=> Resolve(Left, ctx, globals) + Resolve(Right, ctx, globals);

		internal static float Resolve(ValueSource<float> source, TriggerContext ctx, VariableStore globals)
			=> source?.Resolve(ctx, globals) ?? 0f;
	}

	[Serializable]
	[PhrasePreview("clamp {Value} to {Min}..{Max}")]
	[PhraseTemplate("clamp {Value} between {Min} and {Max}")]
	public class ClampFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Value")]
		public ValueSource<float> Value = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Min")]
		public ValueSource<float> Min = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Max")]
		public ValueSource<float> Max = new LiteralValue<float>(1f);

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			float min = AddFloatFunction.Resolve(Min, ctx, globals);
			float max = AddFloatFunction.Resolve(Max, ctx, globals);
			if (max < min) (min, max) = (max, min);
			return Math.Min(Math.Max(AddFloatFunction.Resolve(Value, ctx, globals), min), max);
		}
	}
}
