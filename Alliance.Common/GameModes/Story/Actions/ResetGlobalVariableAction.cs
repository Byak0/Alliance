using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhrasePreview("Reset {VariableName}")]
	[PhraseTemplate("Reset {VariableName}")]
	public class ResetGlobalVariableAction : ActionBase
	{
		[ConfigProperty(label: "Variable Name", tooltip: "Name of the global variable to reset to its default value.")]
		[VariableRef]
		public string VariableName = "";

		public ResetGlobalVariableAction() { }

		public override ActionTask Execute(VariableStore context)
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
				object parsed = SetGlobalVariableAction.ParseValue(def.DefaultValue, def.Type);
				ScenarioManager.Instance.Globals.Set(VariableName, parsed);
			}
			return ActionTask.CompletedTask;
		}
	}
}
