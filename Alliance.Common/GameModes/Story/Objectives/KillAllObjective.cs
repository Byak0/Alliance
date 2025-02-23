using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Objectives
{
	public class KillAllObjective : ObjectiveBase
	{
		private int _enemiesKilled = -1;
		private int _enemiesLeft = -1;

		public KillAllObjective(BattleSideEnum side, LocalizedString name, LocalizedString desc, bool instantWin, bool requiredForWin) :
			base(side, name, desc, instantWin, requiredForWin)
		{
		}

		public KillAllObjective() { }

		public override void RegisterForUpdate()
		{
			ObjectivesBehavior objBehavior = Mission.Current.GetMissionBehavior<ObjectivesBehavior>();
			if (objBehavior == null) return;
			if (Side == BattleSideEnum.Defender) objBehavior.UpdateTotalAttackersKilled += Refresh;
			else if (Side == BattleSideEnum.Attacker) objBehavior.UpdateTotalDefendersKilled += Refresh;
		}

		public override void UnregisterForUpdate()
		{
			ObjectivesBehavior objBehavior = Mission.Current.GetMissionBehavior<ObjectivesBehavior>();
			if (objBehavior == null) return;
			if (Side == BattleSideEnum.Defender) objBehavior.UpdateTotalAttackersKilled -= Refresh;
			else if (Side == BattleSideEnum.Attacker) objBehavior.UpdateTotalDefendersKilled -= Refresh;
		}

		public void Refresh(int nbKilled)
		{
			if (Mission.Current.AttackerTeam == null || Mission.Current.DefenderTeam == null) return;

			_enemiesKilled = nbKilled;
			if (Side == BattleSideEnum.Defender)
			{
				_enemiesLeft = Mission.Current.AttackerTeam.ActiveAgents.Count;
			}
			else
			{
				_enemiesLeft = Mission.Current.DefenderTeam.ActiveAgents.Count;
			}
		}

		public override bool CheckObjective()
		{
			return _enemiesKilled > 0 && _enemiesLeft == 0;
		}

		public override void Reset()
		{
			_enemiesKilled = -1;
			_enemiesLeft = -1;
			Active = true;
		}

		public override string GetProgressAsString()
		{
			return _enemiesKilled >= 0 ? $"{_enemiesKilled}/{_enemiesKilled + _enemiesLeft} killed" : "";
		}
	}
}
