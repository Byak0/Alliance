using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System.Xml.Serialization;
using static Alliance.Common.GameModes.Story.Utilities.ScenarioData;

namespace Alliance.Common.GameModes.Story.Models
{
	[PhrasePreview("{Name} : {DefaultValue}")]
	[PhraseTemplate(
		"Variable {Name} is a",
		"{Type}{?Type=Enum:→{EnumTypeName}} with default value = {DefaultValue}")]
	public class ScenarioVariable
	{
		[ConfigProperty(label: "Name", tooltip: "Unique variable name used to reference it in conditions and actions.")]
		public string Name = "VariableName";

		[ConfigProperty(label: "Type", tooltip: "Data type of the variable.")]
		public VariableType Type = VariableType.Bool;

		[ConfigProperty(label: "Default Value", tooltip: "Default value.")]
		public string DefaultValue = "true";

		[ConfigProperty(label: "Enum Type Name", tooltip: "Name of the enum type (e.g. BattleSideEnum).", dependency: $"?{nameof(Type)}={nameof(VariableType.Enum)}")]
		public string EnumTypeName = "";

		[ConfigProperty(isEditable: false)]
		[XmlIgnore]
		public object RuntimeValue;

		public ScenarioVariable() { }

		public ScenarioVariable(string name, VariableType type, string defaultValue = "")
		{
			Name = name;
			Type = type;
			DefaultValue = defaultValue;
		}
	}
}
