using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using static Alliance.Common.GameModes.Story.Conditions.Condition;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Change mortality status of agents in a zone.
	/// </summary>
	[Serializable]
	[PhrasePreview("Make {Target} from {Side} {State} in...")]
	[PhraseTemplate("Make {Target} from {Side} {State} in {Zone}")]
	public class MortalityStateZoneAction : ActionBase
	{
		public SerializableZone Zone;
		public SideType Side = SideType.All;
		public TargetType Target = TargetType.All;
		[ConfigProperty(label: "Status", tooltip: "Which status is given in this zone.")]
		public MortalityState State = MortalityState.Invulnerable;

		public MortalityStateZoneAction() { }
	}
}