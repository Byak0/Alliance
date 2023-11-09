using Alliance.Common.GameModes.Story.Behaviors;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Models.Objectives
{
    public class TimerObjective : ObjectiveBase
    {
        ObjectivesBehavior ObjectivesBehavior { get; set; }
        public float Duration { get; set; }
        public float RemainingTime { get; set; }

        public TimerObjective(BattleSideEnum side, string name, string desc, bool instantWin, bool requiredForWin, int duration) :
            base(side, name, desc, instantWin, requiredForWin)
        {
            Duration = duration;
            RemainingTime = duration;
        }

        public override void RegisterForUpdate()
        {
            ObjectivesBehavior = Mission.Current?.GetMissionBehavior<ObjectivesBehavior>();
            if (GameNetwork.IsServer) ObjectivesBehavior?.StartTimerAsServer(Duration);
        }

        public override void UnregisterForUpdate()
        {
            ObjectivesBehavior = null;
        }

        public override void Reset()
        {
            Active = true;
            ObjectivesBehavior = Mission.Current?.GetMissionBehavior<ObjectivesBehavior>();
            if (GameNetwork.IsServer) ObjectivesBehavior?.StartTimerAsServer(Duration);
        }

        public override bool CheckObjective()
        {
            if (Mission.Current == null || ObjectivesBehavior == null) return false;
            RemainingTime = ObjectivesBehavior.GetRemainingTime(false);
            return ObjectivesBehavior.HasTimerElapsed();
        }

        public override string GetProgressAsString()
        {
            if (ObjectivesBehavior != null && ObjectivesBehavior.IsTimerRunning)
                return TimeSpan.FromSeconds(RemainingTime).ToString("mm':'ss");
            else
                return "";
        }
    }
}
