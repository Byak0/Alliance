using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Linq;
using static Alliance.Common.GameModes.Story.Utilities.ScenarioData;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhraseTemplate("Set {VariableName} = {Value}")]
	public class SetVariableAction : ActionBase
	{
		[ConfigProperty(label: "Variable Name", tooltip: "Name of the global variable to set.")]
		[VariableRef(typeof(object))]
		public string VariableName = "";

		[ConfigProperty(label: "Value", tooltip: "Value to set (parsed according to the variable's type).")]
		[DependsOnVariable(nameof(VariableName))]
		public string Value = "";

		public SetVariableAction() { }

		public override ActionTask Execute()
		{
			if (string.IsNullOrWhiteSpace(VariableName)) return ActionTask.CompletedTask;

			VariableType varType = ResolveVariableType(VariableName);
			object parsed = ParseValue(Value, varType);
			ScenarioManager.Instance.Globals.Set(VariableName, parsed);
			return ActionTask.CompletedTask;
		}

		private static VariableType ResolveVariableType(string variableName)
		{
			ScenarioVariable def = ScenarioManager.Instance.CurrentScenario?.Variables?
				.FirstOrDefault(v => v.Name == variableName);
			return def?.Type ?? VariableType.String;
		}

		public static object ParseValue(string raw, VariableType type)
		{
			switch (type)
			{
				case VariableType.Int:
					if (int.TryParse(raw, out int i)) return i;
					return 0;
				case VariableType.Float:
					if (float.TryParse(raw, out float f)) return f;
					return 0f;
				case VariableType.Bool:
					if (bool.TryParse(raw, out bool b)) return b;
					return false;
				case VariableType.String:
					return raw ?? "";
				default:
					return raw;
			}
		}
	}
}
