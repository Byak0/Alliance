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

	[Serializable]
	[PhrasePreview("Text: {Template}")]
	public class TextElement : ProgressElement
	{
		public override int ElementType => 0;

		[ConfigProperty(label: "Template", tooltip: "Text with {0}, {1}... placeholders. Each language uses its own version.")]
		public LocalizedString Template = new("{0}");

		[ConfigProperty(label: "Values", tooltip: "Dynamic values to fill placeholders, resolved server-side.")]
		public List<ValueSource> Values = new List<ValueSource>();

		public TextElement() { }

		public TextElement(LocalizedString template, params ValueSource[] values)
		{
			Template = template;
			Values = new List<ValueSource>(values);
		}
	}

	[Serializable]
	[PhrasePreview("Bar: {Label}")]
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
	[PhrasePreview("Timer: {Label}")]
	public class TimerElement : ProgressElement
	{
		public override int ElementType => 2;

		[ConfigProperty(label: "Label")]
		public LocalizedString Label = new("");

		[ConfigProperty(label: "End time (seconds)", tooltip: "Mission time at which the timer reaches zero. Synced once; client ticks locally.")]
		public ValueSource<float> EndTime = new LiteralValue<float>(0f);

		public TimerElement() { }

		public TimerElement(LocalizedString label, ValueSource<float> endTime)
		{
			Label = label;
			EndTime = endTime;
		}
	}
}
