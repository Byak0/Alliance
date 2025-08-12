using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using static Alliance.Common.GameModes.Story.Conditions.Condition;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// change mortality status of agents in a zone.
	/// </summary>
	[Serializable]
	public class MortalityStateZoneAction : ActionBase
	{
		public SerializableZone Zone;
		public SideType Side = SideType.All;
		public TargetType Target = TargetType.All;
		[ScenarioEditor(label: "Status", tooltip: "Which status is given in this zone.")]
		public MortalityState State = MortalityState.Invulnerable;

		public MortalityStateZoneAction() { }
	}
}