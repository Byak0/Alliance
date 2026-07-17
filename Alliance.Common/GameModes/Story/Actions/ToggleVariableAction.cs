using Alliance.Common.Core.Configuration.Models;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhraseTemplate("Toggle {VariableName}")]
	public class ToggleVariableAction : ActionBase
	{
		[ConfigProperty(label: "Variable Name", tooltip: "Name of the boolean global variable to toggle.")]
		[VariableRef(typeof(bool))]
		public string VariableName = "";

		public ToggleVariableAction() { }

		public override ActionTask Execute()
		{
			if (string.IsNullOrWhiteSpace(VariableName)) return ActionTask.CompletedTask;

			bool current = ScenarioManager.Instance.Globals.Get<bool>(VariableName);
			bool newVal = !current;
			ScenarioManager.Instance.Globals.Set(VariableName, newVal);
			return ActionTask.CompletedTask;
		}
	}
}
