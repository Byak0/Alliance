using Alliance.Common.GameModes.Story.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Models.Objectives
{
    public class KillCountObjective : ObjectiveBase
    {
        public int KillCount { get; set; }
        public int KillProgress { get; set; }

        public KillCountObjective(BattleSideEnum side, string name, string desc, bool instantWin, bool requiredForWin, int killCount) :
            base(side, name, desc, instantWin, requiredForWin)
        {
            KillCount = killCount;
            KillProgress = 0;
        }

        public override void RegisterForUpdate()
        {
            ObjectivesBehavior objBehavior = Mission.Current.GetMissionBehavior<ObjectivesBehavior>();
            if (objBehavior == null) return;
            if (Side == BattleSideEnum.Defender) objBehavior.UpdateTotalAttackersKilled += UpdateEnemiesKilled;
            else if (Side == BattleSideEnum.Attacker) objBehavior.UpdateTotalDefendersKilled += UpdateEnemiesKilled;
        }

        public override void UnregisterForUpdate()
        {
            ObjectivesBehavior objBehavior = Mission.Current.GetMissionBehavior<ObjectivesBehavior>();
            if (objBehavior == null) return;
            if (Side == BattleSideEnum.Defender) objBehavior.UpdateTotalAttackersKilled -= UpdateEnemiesKilled;
            else if (Side == BattleSideEnum.Attacker) objBehavior.UpdateTotalDefendersKilled -= UpdateEnemiesKilled;
        }

        public void UpdateEnemiesKilled(int enemiesKilled)
        {
            KillProgress = enemiesKilled;
        }

        public override bool CheckObjective()
        {
            return KillProgress >= KillCount;
        }

        public override void Reset()
        {
            Active = true;
            KillProgress = 0;
        }

        public override string GetProgressAsString()
        {
            return KillProgress + "/" + KillCount;
        }
    }
}
