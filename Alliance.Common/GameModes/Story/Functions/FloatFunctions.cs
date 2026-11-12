using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
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

		public override object Evaluate(VariableStore context)
			=> Resolve(Left, context) + Resolve(Right, context);

		internal static float Resolve(ValueSource<float> source, VariableStore context)
			=> source?.Resolve(context) ?? 0f;
	}

	[Serializable]
	[PhrasePreview("{Left} - {Right}")]
	[PhraseTemplate("{Left} minus {Right}")]
	public class SubtractFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(0f);

		public override object Evaluate(VariableStore context)
			=> AddFloatFunction.Resolve(Left, context) - AddFloatFunction.Resolve(Right, context);
	}

	[Serializable]
	[PhrasePreview("{Left} * {Right}")]
	[PhraseTemplate("{Left} times {Right}")]
	public class MultiplyFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(1f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(1f);

		public override object Evaluate(VariableStore context)
			=> AddFloatFunction.Resolve(Left, context) * AddFloatFunction.Resolve(Right, context);
	}

	[Serializable]
	[PhrasePreview("{Left} / {Right}")]
	[PhraseTemplate("{Left} divided by {Right}")]
	public class DivideFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(1f);

		public override object Evaluate(VariableStore context)
		{
			float right = AddFloatFunction.Resolve(Right, context);
			if (right == 0f) return 0f;
			return AddFloatFunction.Resolve(Left, context) / right;
		}
	}

	[Serializable]
	[PhrasePreview("min({Left}, {Right})")]
	[PhraseTemplate("minimum of {Left} and {Right}")]
	public class MinFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(0f);

		public override object Evaluate(VariableStore context)
			=> Math.Min(AddFloatFunction.Resolve(Left, context), AddFloatFunction.Resolve(Right, context));
	}

	[Serializable]
	[PhrasePreview("max({Left}, {Right})")]
	[PhraseTemplate("maximum of {Left} and {Right}")]
	public class MaxFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(0f);

		public override object Evaluate(VariableStore context)
			=> Math.Max(AddFloatFunction.Resolve(Left, context), AddFloatFunction.Resolve(Right, context));
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

		public override object Evaluate(VariableStore context)
		{
			float min = AddFloatFunction.Resolve(Min, context);
			float max = AddFloatFunction.Resolve(Max, context);
			if (max < min) (min, max) = (max, min);
			return Math.Min(Math.Max(AddFloatFunction.Resolve(Value, context), min), max);
		}
	}

	[Serializable]
	[PhrasePreview("random from {Min} to {Max}")]
	[PhraseTemplate("random float from {Min} to {Max}")]
	public class RandomFloatFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Min")]
		public ValueSource<float> Min = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Max")]
		public ValueSource<float> Max = new LiteralValue<float>(1f);

		private static readonly Random _rng = new Random();

		public RandomFloatFunction() { }

		public override object Evaluate(VariableStore context)
		{
			float min = AddFloatFunction.Resolve(Min, context);
			float max = AddFloatFunction.Resolve(Max, context);
			if (min > max) (min, max) = (max, min);
			return (float)(min + _rng.NextDouble() * (max - min));
		}
	}

	[Serializable]
	[PhrasePreview("{Left} > {Right}")]
	[PhraseTemplate("{Left} is greater than {Right}")]
	public class GreaterThanFloatFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(0f);

		public override object Evaluate(VariableStore context)
			=> AddFloatFunction.Resolve(Left, context) > AddFloatFunction.Resolve(Right, context);
	}

	[Serializable]
	[PhrasePreview("{Left} < {Right}")]
	[PhraseTemplate("{Left} is less than {Right}")]
	public class LessThanFloatFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(0f);

		public override object Evaluate(VariableStore context)
			=> AddFloatFunction.Resolve(Left, context) < AddFloatFunction.Resolve(Right, context);
	}

	[Serializable]
	[PhrasePreview("{Left} = {Right}")]
	[PhraseTemplate("{Left} equals {Right}")]
	public class EqualsFloatFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<float> Left = new LiteralValue<float>(0f);
		[ConfigProperty(label: "Right")]
		public ValueSource<float> Right = new LiteralValue<float>(0f);

		public override object Evaluate(VariableStore context)
			=> Math.Abs(AddFloatFunction.Resolve(Left, context) - AddFloatFunction.Resolve(Right, context)) < float.Epsilon;
	}

	[Serializable]
	[PhrasePreview("floor({Value})")]
	[PhraseTemplate("floor of {Value}")]
	public class FloorFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Value")]
		public ValueSource<float> Value = new LiteralValue<float>(0f);

		public FloorFunction() { }

		public override object Evaluate(VariableStore context)
			=> (int)Math.Floor(AddFloatFunction.Resolve(Value, context));
	}

	[Serializable]
	[PhrasePreview("ceil({Value})")]
	[PhraseTemplate("ceiling of {Value}")]
	public class CeilFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Value")]
		public ValueSource<float> Value = new LiteralValue<float>(0f);

		public CeilFunction() { }

		public override object Evaluate(VariableStore context)
			=> (int)Math.Ceiling(AddFloatFunction.Resolve(Value, context));
	}

	[Serializable]
	[PhrasePreview("round({Value})")]
	[PhraseTemplate("round {Value} to nearest integer")]
	public class RoundFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Value")]
		public ValueSource<float> Value = new LiteralValue<float>(0f);

		public RoundFunction() { }

		public override object Evaluate(VariableStore context)
			=> (int)Math.Round(AddFloatFunction.Resolve(Value, context));
	}
}
