using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
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
		[InlineContent]
		public Condition CompletionCondition;

		[ConfigProperty(label: "Progress display", tooltip: "How progress is shown to players. Leave empty for hidden objectives.", category: "Progress")]
		public List<ProgressElement> Progress = new List<ProgressElement>();

		[XmlIgnore]
		public bool Active = true;

		public bool IsCompleted => !Active;

		public Objective() { }

		public Objective(BattleSideEnum side, LocalizedString name, LocalizedString desc, bool instantWin, bool optional, Condition condition)
		{
			Side = side;
			Name = name;
			Description = desc;
			InstantActWin = instantWin;
			Optional = optional;
			CompletionCondition = condition;
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
	}
}
