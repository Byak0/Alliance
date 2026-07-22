using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Alliance.Common.GameModes.Story.Functions
{
	[Serializable]
	[PhrasePreview("concat {Values}")]
	public class ConcatFunction : Function
	{
		public override Type ReturnType => typeof(string);

		[ConfigProperty(label: "Values", tooltip: "Strings to concatenate in order.")]
		public List<ValueSource<string>> Values = new List<ValueSource<string>>();

		public ConcatFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			StringBuilder sb = new StringBuilder();
			foreach (ValueSource<string> vs in Values)
			{
				sb.Append(vs?.Resolve(ctx, globals) ?? "");
			}
			return sb.ToString();
		}
	}

	[Serializable]
	[PhrasePreview("format '{Template}' with {Value}")]
	[PhraseTemplate("format '{Template}' with {Value}")]
	public class FormatStringFunction : Function
	{
		public override Type ReturnType => typeof(string);

		[ConfigProperty(label: "Template", tooltip: "String template. Use {0} as placeholder for the value.")]
		public ValueSource<string> Template = new LiteralValue<string>("{0}");
		[ConfigProperty(label: "Value", tooltip: "Value to insert into the template.")]
		public ValueSource<string> Value = new LiteralValue<string>("");

		public FormatStringFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			string template = Template?.Resolve(ctx, globals) ?? "{0}";
			string value = Value?.Resolve(ctx, globals) ?? "";
			return string.Format(template, value);
		}
	}

	[Serializable]
	[PhrasePreview("substring of {Value} from {Start} length {Length}")]
	[PhraseTemplate("substring of {Value} from {Start} length {Length}")]
	public class SubstringFunction : Function
	{
		public override Type ReturnType => typeof(string);

		[ConfigProperty(label: "Value")]
		public ValueSource<string> Value = new LiteralValue<string>("");
		[ConfigProperty(label: "Start", tooltip: "Start index (0-based).")]
		public ValueSource<int> Start = new LiteralValue<int>(0);
		[ConfigProperty(label: "Length", tooltip: "Number of characters to take. 0 = take all.")]
		public ValueSource<int> Length = new LiteralValue<int>(0);

		public SubstringFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			string value = Value?.Resolve(ctx, globals) ?? "";
			int start = AddIntFunction.Resolve(Start, ctx, globals);
			int length = AddIntFunction.Resolve(Length, ctx, globals);
			if (start < 0 || start >= value.Length) return "";
			if (length <= 0) return value.Substring(start);
			return value.Substring(start, Math.Min(length, value.Length - start));
		}
	}

	[Serializable]
	[PhrasePreview("length of {Value}")]
	[PhraseTemplate("length of {Value}")]
	public class StringLengthFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Value")]
		public ValueSource<string> Value = new LiteralValue<string>("");

		public StringLengthFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			string value = Value?.Resolve(ctx, globals) ?? "";
			return value.Length;
		}
	}

	[Serializable]
	[PhrasePreview("upper case of {Value}")]
	[PhraseTemplate("upper case of {Value}")]
	public class ToUpperFunction : Function
	{
		public override Type ReturnType => typeof(string);

		[ConfigProperty(label: "Value")]
		public ValueSource<string> Value = new LiteralValue<string>("");

		public ToUpperFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			string value = Value?.Resolve(ctx, globals) ?? "";
			return value.ToUpper();
		}
	}

	[Serializable]
	[PhrasePreview("lower case of {Value}")]
	[PhraseTemplate("lower case of {Value}")]
	public class ToLowerFunction : Function
	{
		public override Type ReturnType => typeof(string);

		[ConfigProperty(label: "Value")]
		public ValueSource<string> Value = new LiteralValue<string>("");

		public ToLowerFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			string value = Value?.Resolve(ctx, globals) ?? "";
			return value.ToLower();
		}
	}

	[Serializable]
	[PhrasePreview("{Value} starts with {Prefix}")]
	[PhraseTemplate("{Value} starts with {Prefix}")]
	public class StartsWithFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Value")]
		public ValueSource<string> Value = new LiteralValue<string>("");
		[ConfigProperty(label: "Prefix")]
		public ValueSource<string> Prefix = new LiteralValue<string>("");

		public StartsWithFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			string value = Value?.Resolve(ctx, globals) ?? "";
			string prefix = Prefix?.Resolve(ctx, globals) ?? "";
			return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Serializable]
	[PhrasePreview("{Value} contains {Substring}")]
	[PhraseTemplate("{Value} contains {Substring}")]
	public class ContainsStringFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Value")]
		public ValueSource<string> Value = new LiteralValue<string>("");
		[ConfigProperty(label: "Substring")]
		public ValueSource<string> Substring = new LiteralValue<string>("");

		public ContainsStringFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			string value = Value?.Resolve(ctx, globals) ?? "";
			string substr = Substring?.Resolve(ctx, globals) ?? "";
			return value.IndexOf(substr, StringComparison.OrdinalIgnoreCase) >= 0;
		}
	}
}
