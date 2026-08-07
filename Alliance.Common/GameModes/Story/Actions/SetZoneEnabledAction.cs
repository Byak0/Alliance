using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Linq;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Actions
{
	[Serializable]
	[PhrasePreview("{State} zone {ZoneName}")]
	[PhraseTemplate("{State} zone {ZoneName}")]
	public class SetZoneEnabledAction : ActionBase
	{
		public enum ZoneState
		{
			Enable,
			Disable,
			Toggle
		}

		[ConfigProperty(label: "Zone", tooltip: "Name of a zone from this act's Zones list.")]
		[VariableRef(typeof(Zone))]
		public string ZoneName = "";

		[ConfigProperty(label: "State", tooltip: "Action to perform on the zone. Toggle switches between enabled and disabled.")]
		public ZoneState State;

		public SetZoneEnabledAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			Act act = ScenarioManager.Instance.CurrentAct;
			NamedZone zone = act?.Zones.FirstOrDefault(z => z?.Name == ZoneName);
			if (zone == null)
			{
				Log($"Error in SetZoneEnabledAction - Zone \"{ZoneName}\" not found on current act.", LogLevel.Error);
				return ActionTask.CompletedTask;
			}

			switch (State)
			{
				case ZoneState.Enable:
					zone.Enabled = true;
					break;
				case ZoneState.Disable:
					zone.Enabled = false;
					break;
				case ZoneState.Toggle:
					zone.Enabled = !zone.Enabled;
					break;
			}

			return ActionTask.CompletedTask;
		}
	}
}
