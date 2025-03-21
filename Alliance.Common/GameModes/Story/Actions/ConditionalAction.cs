using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
		[ScenarioEditor(label: "Delay", tooltip: "Delay in seconds before executing the \"Actions if true\".")]
		public float Delay = 0f;

		public ConditionalAction(Condition condition, ActionBase actionIfTrue, ActionBase actionIfFalse, float delay = 0f)
		{
			Condition = new List<Condition> { condition };
			ActionIfTrue = new List<ActionBase> { actionIfTrue };
			ActionIfFalse = new List<ActionBase> { actionIfFalse };
			Delay = delay;
		}

		public ConditionalAction()
		{
			Condition = new List<Condition>();
			ActionIfTrue = new List<ActionBase>();
			ActionIfFalse = new List<ActionBase>();
			Delay = 0f;
		}

		public override void Execute()
		{
			// Get result of all conditions (AND)
			bool result = true;
			Condition.ForEach(c => result &= c.Evaluate(ScenarioManager.Instance));

			// Execute actions based on result
			if (result)
			{
				if (Delay > 0f)
				{
					DelayedExecute();
				}
				else
				{
					ActionIfTrue.ForEach(a => a.Execute());
				}
			}
			else
			{
				ActionIfFalse.ForEach(a => a.Execute());
			}
		}

		private async void DelayedExecute()
		{
			await Task.Delay((int)(Delay * 1000));
			ActionIfTrue.ForEach(a => a.Execute());
		}
	}
}