using Alliance.Common.Extensions.FlagsTracker.Scripts;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.FlagsTracker.Behaviors
{
	public class CapturableZoneBehavior : MissionNetwork, IMissionBehavior
	{
		public List<CS_CapturableZone> CapturableZones { get; private set; }

		private float _delay;

		public CapturableZoneBehavior()
		{
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			CapturableZones = Mission.Current.MissionObjects.FindAllWithType<CS_CapturableZone>().ToList();
			Log($"Found {CapturableZones.Count} capturable zones : ", LogLevel.Debug);
			foreach (CS_CapturableZone capturableZone in CapturableZones)
			{
				Log($"{capturableZone.ZoneName} - {capturableZone.ZoneId} - Owned by {capturableZone.Owner}", LogLevel.Debug);
			}
		}
	}
}
