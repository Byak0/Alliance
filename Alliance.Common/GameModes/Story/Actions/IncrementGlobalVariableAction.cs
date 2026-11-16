using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhrasePreview("Increment {VariableName} by {Amount}")]
	[PhraseTemplate("Increment {VariableName} by {Amount}")]
	public class IncrementGlobalVariableAction : ActionBase
	{
		[ConfigProperty(label: "Variable Name", tooltip: "Name of the global variable to increment.")]
		[VariableRef(typeof(float))]
		public string VariableName = "";

		[ConfigProperty(label: "Amount", tooltip: "Amount to add (integer or float).")]
		[DependsOnVariable("VariableName")]
		public float Amount = 1f;

		public IncrementGlobalVariableAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			if (string.IsNullOrWhiteSpace(VariableName)) return ActionTask.CompletedTask;

			object current = ScenarioManager.Instance.Globals.Get<object>(VariableName);
			float parsed;
			if (current is int intVal)
				parsed = intVal;
			else if (current is float floatVal)
				parsed = floatVal;
			else
				parsed = 0f;

			float newVal = parsed + Amount;

			if (current is int)
				ScenarioManager.Instance.Globals.Set(VariableName, (int)newVal);
			else
				ScenarioManager.Instance.Globals.Set(VariableName, newVal);

			return ActionTask.CompletedTask;
		}
	}
}
