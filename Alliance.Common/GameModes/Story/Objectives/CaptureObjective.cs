using Alliance.Common.Extensions.FlagsTracker.Scripts;
using Alliance.Common.GameModes.Story.Models;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Objectives
{
	public class CaptureObjective : ObjectiveBase
	{
		public string CapturableZoneId;

		private CS_CapturableZone _capturableZone;

		public CaptureObjective(BattleSideEnum side, LocalizedString name, LocalizedString desc, bool instantWin, bool requiredForWin, string capturableZoneId) :
			base(side, name, desc, instantWin, requiredForWin)
		{
			CapturableZoneId = capturableZoneId;
		}

		public CaptureObjective() { }

		public override void RegisterForUpdate()
		{
			_capturableZone = Mission.Current.MissionObjects.FindAllWithType<CS_CapturableZone>().FirstOrDefault(cz => cz.ZoneId.ToLower() == CapturableZoneId.ToLower());
			Log($"Registered {_capturableZone?.ZoneName} capturable zone.", LogLevel.Debug);
		}

		public override void UnregisterForUpdate()
		{
			_capturableZone = null;
		}

		public override void Reset()
		{
			Active = true;
		}

		public override bool CheckObjective()
		{
			if (_capturableZone == null) return false;
			return _capturableZone.Owner == Side;
		}

		public override string GetProgressAsString()
		{
			if (_capturableZone == null) return "";
			int distance = (int?)Agent.Main?.Frame.origin.Distance(_capturableZone.Position) ?? -1;
			string result = distance != -1 ? $"{distance}m" : "?";
			return $"{_capturableZone.ZoneName} - {result}";
		}
	}
}
