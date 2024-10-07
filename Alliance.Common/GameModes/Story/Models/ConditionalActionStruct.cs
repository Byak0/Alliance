using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// A conditional action is a set of conditions and actions that are triggered when the conditions are met.
	/// </summary>
	public class ConditionalActionStruct
	{
		public string Name = "Conditional Action";
		[ScenarioEditor(label: "Conditions", tooltip: "If multiple conditions are set, they must all be true to trigger the actions.")]
		public List<Condition> Conditions = new List<Condition>();
		[ScenarioEditor(label: "Actions", tooltip: "Actions triggered when conditions are met.")]
		public List<ActionBase> Actions = new List<ActionBase>();
		[ScenarioEditor(label: "Enabled", tooltip: "Enable or disable the conditional action.")]
		public bool Enabled = true;
		[ScenarioEditor(label: "One Time Only", tooltip: "If true, the conditional action will only trigger once.")]
		public bool OneTimeOnly = false;
		[ScenarioEditor(label: "Refresh Delay", tooltip: "Delay between condition checks in seconds. Longer delays are preferable for performance.")]
		public float RefreshDelay = 1f;
		[ScenarioEditor(isEditable: false)]
		internal float _refreshTimer = 0f;

		public ConditionalActionStruct() { }

		/// <summary>
		/// Check if the conditions are met and execute the actions if they are.
		/// </summary>
		public void Tick(float dt)
		{
			if (Enabled)
			{
				_refreshTimer += dt;
				if (_refreshTimer < RefreshDelay) return;
				_refreshTimer = 0f;

				bool conditionsMet = true;
				foreach (Condition condition in Conditions)
				{
					if (!condition.Evaluate(ScenarioManager.Instance))
					{
						conditionsMet = false;
						break;
					}
				}

				if (conditionsMet)
				{
					foreach (ActionBase action in Actions)
					{
						action.Execute();
					}

					if (OneTimeOnly)
					{
						Enabled = false;
					}
				}
			}
		}
	}
}
