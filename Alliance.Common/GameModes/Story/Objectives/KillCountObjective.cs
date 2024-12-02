using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Objectives
{
	public class KillCountObjective : ObjectiveBase
	{
		public int KillCount;

		private int _killProgress;

		public KillCountObjective(BattleSideEnum side, LocalizedString name, LocalizedString desc, bool instantWin, bool requiredForWin, int killCount) :
			base(side, name, desc, instantWin, requiredForWin)
		{
			KillCount = killCount;
			_killProgress = 0;
		}

		public KillCountObjective() { }

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
			_killProgress = enemiesKilled;
		}

		public override bool CheckObjective()
		{
			return _killProgress >= KillCount;
		}

		public override void Reset()
		{
			Active = true;
			_killProgress = 0;
		}

		public override string GetProgressAsString()
		{
			return _killProgress + "/" + KillCount;
		}
	}
}
