using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.GameModes.Story.Validation;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Objectives
{
	[Serializable]
	[PhrasePreview("{Name}")]
	public class Objective
	{
		[ConfigProperty(label: "Owner side", tooltip: "Which team this objective belongs to.")]
		public BattleSideEnum Side = BattleSideEnum.Attacker;

		[ConfigProperty(label: "Name")]
		public LocalizedString Name = new("Objective");

		[ConfigProperty(label: "Description")]
		public LocalizedString Description = new("");

		[ConfigProperty(label: "Optional", tooltip: "Optional objectives are excluded from the win condition but can still trigger Instant Win.")]
		public bool Optional = false;

		[ConfigProperty(label: "Instant win", tooltip: "If enabled, completing this objective immediately wins the act for its side.")]
		public bool InstantActWin = true;

		[ConfigProperty(label: "Completion condition", tooltip: "When this condition is met, the objective is completed.")]
		public Condition CompletionCondition;

		[ConfigProperty(label: "Progress display", tooltip: "How progress is shown to players. Leave empty for hidden objectives.")]
		public List<ProgressElement> Progress = new List<ProgressElement>();

		[XmlIgnore]
		public bool Active = true;

		public bool IsCompleted => !Active;

		public Objective() { }

		public Objective(BattleSideEnum side, LocalizedString name, LocalizedString desc,
			bool instantWin, bool optional, Condition condition, List<ProgressElement> progress = null)
		{
			Side = side;
			Name = name;
			Description = desc;
			InstantActWin = instantWin;
			Optional = optional;
			CompletionCondition = condition;
			if (progress != null) Progress = progress;
		}

		public void Reset()
		{
			Active = true;
			CompletionCondition?.Register(WeakGameEntity.Invalid);
		}

		public void Unregister()
		{
			CompletionCondition?.Unregister();
		}

		public bool Check(VariableStore context)
		{
			if (!Active) return true;
			if (CompletionCondition?.Evaluate(context) ?? false)
			{
				Active = false;
				return true;
			}
			return false;
		}

		private static readonly Regex PlaceholderRegex = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);

		public List<ValidationIssue> Validate(string path)
		{
			var issues = new List<ValidationIssue>();

			if (CompletionCondition == null)
				issues.Add(new ValidationIssue(ValidationSeverity.Error, path, "No completion condition set."));

			if (Progress != null)
			{
				foreach (var el in Progress)
				{
					if (el is TextElement text)
					{
						int valueCount = text.Values?.Count ?? 0;
						string template = text.Template?.GetText("English") ?? "";

						var matches = PlaceholderRegex.Matches(template);
						int highestPlaceholder = -1;
						foreach (Match m in matches)
						{
							int idx = int.Parse(m.Groups[1].Value);
							if (idx > highestPlaceholder) highestPlaceholder = idx;
						}

						if (highestPlaceholder >= 0 && highestPlaceholder >= valueCount)
							issues.Add(new ValidationIssue(ValidationSeverity.Warning, path,
								$"TextElement template references {{{highestPlaceholder}}} but only {valueCount} value(s) provided."));

						if (valueCount > 0 && string.IsNullOrWhiteSpace(template))
							issues.Add(new ValidationIssue(ValidationSeverity.Warning, path,
								"TextElement has values but an empty template."));
					}
					else if (el is BarElement bar)
					{
						float min = bar.Min?.Resolve(null) ?? 0f;
						float max = bar.Max?.Resolve(null) ?? 100f;
						if (min > max)
							issues.Add(new ValidationIssue(ValidationSeverity.Warning, path,
								$"BarElement has Min ({min}) > Max ({max})."));
					}
				}
			}

			return issues;
		}
	}
}
