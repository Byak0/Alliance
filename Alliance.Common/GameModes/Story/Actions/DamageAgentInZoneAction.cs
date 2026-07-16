using Alliance.Common.GameModes.Story.Models;
using System;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Damage agents in a zone.
	/// </summary>
	[Serializable]
	[PhrasePreview("Deal {Damage} damage to...")]
	[PhraseTemplate("Deal {Damage} damage to {Target} from {Side} in {Zone}")]
	public class DamageAgentInZoneAction : ActionBase
	{
		public SerializableZone Zone;
		public SideType Side = SideType.All;
		public TargetType Target = TargetType.All;
		public int Damage = 500;

		public DamageAgentInZoneAction() { }
	}
}