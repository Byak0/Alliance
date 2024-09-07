using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Conditional action.
	/// </summary>
	[Serializable]
	public class ConditionalAction : ActionBase
	{
		[ScenarioEditor(label: "Conditions", tooltip: "If there are multiple conditions, they must all be met.")]
		public List<Condition> Condition;
		[ScenarioEditor(label: "Actions if true", tooltip: "Actions to execute if all conditions are met.")]
		public List<ActionBase> ActionIfTrue;
		[ScenarioEditor(label: "Actions if false", tooltip: "Actions to execute if any condition is not met.")]
		public List<ActionBase> ActionIfFalse;

		public ConditionalAction(Condition condition, ActionBase actionIfTrue, ActionBase actionIfFalse)
		{
			Condition = new List<Condition> { condition };
			ActionIfTrue = new List<ActionBase> { actionIfTrue };
			ActionIfFalse = new List<ActionBase> { actionIfFalse };
		}

		public ConditionalAction() { }

		public override void Execute()
		{
			// Get result of all conditions (AND)
			bool result = true;
			Condition.ForEach(c => result &= c.Evaluate(ScenarioManager.Instance));

			// Execute actions based on result
			if (result)
			{
				ActionIfTrue.ForEach(a => a.Execute());
			}
			else
			{
				ActionIfFalse.ForEach(a => a.Execute());
			}
		}
	}
}