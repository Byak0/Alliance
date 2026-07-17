using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhraseTemplate("Reset {VariableName}")]
	public class ResetVariableAction : ActionBase
	{
		[ConfigProperty(label: "Variable Name", tooltip: "Name of the global variable to reset to its default value.")]
		[VariableRef(typeof(object))]
		public string VariableName = "";

		public ResetVariableAction() { }

		public override ActionTask Execute()
		{
			if (string.IsNullOrWhiteSpace(VariableName)) return ActionTask.CompletedTask;

			ScenarioVariable def = null;
			if (ScenarioManager.Instance.CurrentScenario?.Variables != null)
			{
				foreach (var v in ScenarioManager.Instance.CurrentScenario.Variables)
				{
					if (v.Name == VariableName)
					{
						def = v;
						break;
					}
				}
			}

			if (def != null)
			{
				object parsed = SetVariableAction.ParseValue(def.DefaultValue, def.Type);
				ScenarioManager.Instance.Globals.Set(VariableName, parsed);
			}
			return ActionTask.CompletedTask;
		}
	}
}
