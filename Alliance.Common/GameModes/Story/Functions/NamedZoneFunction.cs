using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;

namespace Alliance.Common.GameModes.Story.Functions
{
	// TODO à suppprimer
	/// <summary>
	/// Resolves a zone by its name on the enclosing <see cref="Act"/>'s <c>Zones</c> list. Migrates the
	/// former <c>NamedZoneRef</c> <c>ZoneSource</c> into the unified <see cref="Function"/> model.
	/// </summary>
	//[Serializable]
	//[PhrasePreview("named zone '{ZoneName}'")]
	//[PhraseTemplate("named zone {ZoneName}")]
	//public class NamedZoneFunction : Function
	//{
	//	public override Type ReturnType => typeof(Zone);

	//	[ConfigProperty(label: "Zone name", tooltip: "Name of a zone defined on the enclosing Act.")]
	//	public string ZoneName = "";

	//	public NamedZoneFunction() { }

	//	public override object Evaluate(TriggerContext ctx, VariableStore globals)
	//	{
	//		if (!ScenarioManager.IsInitialized) return null;
	//		return ScenarioManager.Instance.CurrentAct?.FindNamedZone(ZoneName);
	//	}
	//}
}
