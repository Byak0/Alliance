using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Damage agents in a zone.
	/// </summary>
	[Serializable]
	public class VOIPRangeInZoneAction : ActionBase
	{
		public SerializableZone Zone;
		public SideType Side = SideType.All;
		public TargetType Target = TargetType.All;
		public int VOIP_Range = AgentsInfoModel.DEFAULT_SPEAKING_RANGE;

		public VOIPRangeInZoneAction() { }
	}
}