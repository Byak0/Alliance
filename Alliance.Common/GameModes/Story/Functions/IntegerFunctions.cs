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
	public class AddIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(VariableStore context)
			=> Resolve(Left, context) + Resolve(Right, context);

		internal static int Resolve(ValueSource<int> source, VariableStore context)
			=> source?.Resolve(context) ?? 0;
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

		public override object Evaluate(VariableStore context)
			=> AddIntFunction.Resolve(Left, context) - AddIntFunction.Resolve(Right, context);
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

		public override object Evaluate(VariableStore context)
			=> Math.Min(AddIntFunction.Resolve(Left, context), AddIntFunction.Resolve(Right, context));
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

		public override object Evaluate(VariableStore context)
			=> Math.Max(AddIntFunction.Resolve(Left, context), AddIntFunction.Resolve(Right, context));
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

		public override object Evaluate(VariableStore context)
		{
			int min = AddIntFunction.Resolve(Min, context);
			int max = AddIntFunction.Resolve(Max, context);
			if (max < min) (min, max) = (max, min);
			return Math.Min(Math.Max(AddIntFunction.Resolve(Value, context), min), max);
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

		public override object Evaluate(VariableStore context)
			=> AddIntFunction.Resolve(Left, context) > AddIntFunction.Resolve(Right, context);
	}

	[Serializable]
	[PhrasePreview("{Left} * {Right}")]
	[PhraseTemplate("{Left} times {Right}")]
	public class MultiplyIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(1);

		public override object Evaluate(VariableStore context)
			=> AddIntFunction.Resolve(Left, context) * AddIntFunction.Resolve(Right, context);
	}

	[Serializable]
	[PhrasePreview("{Left} / {Right}")]
	[PhraseTemplate("{Left} divided by {Right}")]
	public class DivideIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(1);

		public override object Evaluate(VariableStore context)
		{
			int right = AddIntFunction.Resolve(Right, context);
			if (right == 0) return 0;
			return AddIntFunction.Resolve(Left, context) / right;
		}
	}

	[Serializable]
	[PhrasePreview("{Left} = {Right}")]
	[PhraseTemplate("{Left} equals {Right}")]
	public class EqualsIntFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(VariableStore context)
			=> AddIntFunction.Resolve(Left, context) == AddIntFunction.Resolve(Right, context);
	}

	[Serializable]
	[PhrasePreview("random from {Min} to {Max}")]
	[PhraseTemplate("random integer from {Min} to {Max}")]
	public class RandomIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Min")]
		public ValueSource<int> Min = new LiteralValue<int>(0);
		[ConfigProperty(label: "Max")]
		public ValueSource<int> Max = new LiteralValue<int>(100);

		private static readonly Random _rng = new Random();

		public RandomIntFunction() { }

		public override object Evaluate(VariableStore context)
		{
			int min = AddIntFunction.Resolve(Min, context);
			int max = AddIntFunction.Resolve(Max, context);
			if (min > max) (min, max) = (max, min);
			return _rng.Next(min, max + 1);
		}
	}

	[Serializable]
	[PhrasePreview("{Left} mod {Right}")]
	[PhraseTemplate("{Left} modulo {Right}")]
	public class ModuloIntFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(1);

		public override object Evaluate(VariableStore context)
		{
			int right = AddIntFunction.Resolve(Right, context);
			if (right == 0) return 0;
			return AddIntFunction.Resolve(Left, context) % right;
		}
	}

	[Serializable]
	[PhrasePreview("{Left} < {Right}")]
	[PhraseTemplate("{Left} is less than {Right}")]
	public class LessThanIntFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Left")]
		public ValueSource<int> Left = new LiteralValue<int>(0);
		[ConfigProperty(label: "Right")]
		public ValueSource<int> Right = new LiteralValue<int>(0);

		public override object Evaluate(VariableStore context)
			=> AddIntFunction.Resolve(Left, context) < AddIntFunction.Resolve(Right, context);
	}
}
