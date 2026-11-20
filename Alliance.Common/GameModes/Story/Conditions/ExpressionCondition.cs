using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Evaluates a boolean expression tree. This is the bridge that makes boolean functions useful in a
	/// ScriptedEvent condition list without introducing a one-off condition class for every comparison.
	/// </summary>
	[Serializable]
	[PhrasePreview("when {Value}")]
	[PhraseTemplate("when {Value}")]
	public class ExpressionCondition : Condition
	{
		[ConfigProperty(label: "Value", tooltip: "Boolean literal, variable, or function expression.")]
		public ValueSource<bool> Value = new LiteralValue<bool>(true);

		public override bool Evaluate(VariableStore context)
		{
			return Value?.Resolve(context) ?? false;
		}
	}
}
