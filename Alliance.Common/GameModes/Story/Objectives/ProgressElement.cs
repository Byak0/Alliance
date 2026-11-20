using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Objectives
{
	[Serializable]
	public abstract class ProgressElement
	{
		public abstract int ElementType { get; }
	}

	// ── Progress value wrappers ───────────────────────────────────────
	//
	// The editor can't create elements of List<ValueSource> (abstract, non-generic).
	// These small concrete wrappers give the type picker something to show, and each
	// wraps a typed ValueSource<T> that the editor already knows how to edit.

	[Serializable]
	public abstract class ProgressValue
	{
		public abstract object Resolve(VariableStore context, VariableStore globals);
	}

	[Serializable]
	[PhrasePreview("{Source}")]
	public class IntValue : ProgressValue
	{
		[ConfigProperty(label: "Source")]
		[InlineContent]
		public ValueSource<int> Source = new LiteralValue<int>(0);

		public IntValue() { }
		public IntValue(ValueSource<int> source) { Source = source; }

		public override object Resolve(VariableStore context, VariableStore globals) => Source?.Resolve(context, globals) ?? 0;
	}

	[Serializable]
	[PhrasePreview("{Source}")]
	public class FloatValue : ProgressValue
	{
		[ConfigProperty(label: "Source")]
		[InlineContent]
		public ValueSource<float> Source = new LiteralValue<float>(0f);

		public FloatValue() { }
		public FloatValue(ValueSource<float> source) { Source = source; }

		public override object Resolve(VariableStore context, VariableStore globals) => Source?.Resolve(context, globals) ?? 0f;
	}

	[Serializable]
	[PhrasePreview("{Source}")]
	public class TextValue : ProgressValue
	{
		[ConfigProperty(label: "Source")]
		[InlineContent]
		public ValueSource<string> Source = new LiteralValue<string>("");

		public TextValue() { }
		public TextValue(ValueSource<string> source) { Source = source; }

		public override object Resolve(VariableStore context, VariableStore globals) => Source?.Resolve(context, globals) ?? "";
	}

	// ── Progress elements ─────────────────────────────────────────────

	[Serializable]
	[PhrasePreview("{Template}")]
	public class TextElement : ProgressElement
	{
		public override int ElementType => 0;

		[ConfigProperty(label: "Text", tooltip: "Text displayed to players. You can use {0}, {1} placeholders and define variables to replace them.")]
		public LocalizedString Template = new("{0}");

		[ConfigProperty(label: "Variables", tooltip: "Dynamic values to fill {0}, {1}... placeholders.")]
		public List<ProgressValue> Values = new List<ProgressValue>();

		public TextElement() { }

		public TextElement(LocalizedString template, params ProgressValue[] values)
		{
			Template = template;
			Values = new List<ProgressValue>(values);
		}
	}

	[Serializable]
	[PhrasePreview("Progress bar - {Label} - {Min}/{Current}/{Max}")]
	public class BarElement : ProgressElement
	{
		public override int ElementType => 1;

		[ConfigProperty(label: "Label", tooltip: "Text shown above the bar.")]
		public LocalizedString Label = new("");

		[ConfigProperty(label: "Current")]
		public ValueSource<float> Current = new LiteralValue<float>(0f);

		[ConfigProperty(label: "Min", tooltip: "Minimum value of the bar (default 0).")]
		public ValueSource<float> Min = new LiteralValue<float>(0f);

		[ConfigProperty(label: "Max", tooltip: "Maximum value of the bar.")]
		public ValueSource<float> Max = new LiteralValue<float>(100f);

		public BarElement() { }

		public BarElement(LocalizedString label, ValueSource<float> current, ValueSource<float> min, ValueSource<float> max)
		{
			Label = label;
			Current = current;
			Min = min;
			Max = max;
		}
	}

	[Serializable]
	[PhrasePreview("Timer - {Label} - {EndTime}")]
	public class TimerElement : ProgressElement
	{
		public override int ElementType => 2;

		[ConfigProperty(label: "Label")]
		public LocalizedString Label = new("");

		[ConfigProperty(label: "Timer duration (seconds)")]
		public ValueSource<float> EndTime = new LiteralValue<float>(0f);

		public TimerElement() { }

		public TimerElement(LocalizedString label, ValueSource<float> endTime)
		{
			Label = label;
			EndTime = endTime;
		}
	}
}
