using Alliance.Common.Extensions.FlagsTracker.Scripts;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Models.Objectives
{
    public class CaptureObjective : ObjectiveBase
    {
        public string CapturableZoneId { get; set; }
        public CS_CapturableZone CapturableZone { get; set; }

        public CaptureObjective(BattleSideEnum side, string name, string desc, bool instantWin, bool requiredForWin, string capturableZoneId) :
            base(side, name, desc, instantWin, requiredForWin)
        {
            CapturableZoneId = capturableZoneId;
        }

        public override void RegisterForUpdate()
        {
            CapturableZone = Mission.Current.MissionObjects.FindAllWithType<CS_CapturableZone>().FirstOrDefault(cz => cz.ZoneId == CapturableZoneId);
            Log($"Registered {CapturableZone?.ZoneName} capturable zone.", LogLevel.Debug);
        }

        public override void UnregisterForUpdate()
        {
            CapturableZone = null;
        }

        public override void Reset()
        {
            Active = true;
        }

        public override bool CheckObjective()
        {
            if (CapturableZone == null) return false;
            return CapturableZone.Owner == Side;
        }

        public override string GetProgressAsString()
        {
            if (CapturableZone == null) return "";
            int distance = (int?)Agent.Main?.Frame.origin.Distance(CapturableZone.Position) ?? -1;
            string result = distance != -1 ? $"{distance}m" : "?";
            return $"{CapturableZone.ZoneName} - {result}";
        }
    }
}
