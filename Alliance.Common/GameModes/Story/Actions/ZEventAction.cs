using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Models;
using System;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Some special actions for ZEvent.
	/// </summary>
	[Serializable]
	public class ZEventAction : ActionBase
	{
		public SerializableZone Zone;
		public SideType Side = SideType.All;
		public TargetType Target = TargetType.All;

		public enum ActionType
		{
			Tutut,
			LabyrintheCompleted,
			LabyrintheAbandonned
		}

		[ConfigProperty(label: "Action", tooltip: "Which action to trigger.")]
		public ActionType Action = ActionType.Tutut;

		public ZEventAction() { }
	}
}