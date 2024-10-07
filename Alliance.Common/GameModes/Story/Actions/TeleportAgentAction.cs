using Alliance.Common.GameModes.Story.Models;
using System;
using static Alliance.Common.GameModes.Story.Conditions.AgentEnteredZoneCondition;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Teleport agents from one zone to another.
	/// </summary>
	[Serializable]
	public class TeleportAgentAction : ActionBase
	{
		public SerializableZone StartingZone;
		public SerializableZone DestinationZone;
		public SideType Side = SideType.All;
		public TargetType Target = TargetType.All;

		public TeleportAgentAction() { }
	}
}