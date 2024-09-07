using Alliance.Common.GameModes.Story.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Objectives
{
	public class KillAllObjective : ObjectiveBase
	{
		private int _enemiesLeft;

		public KillAllObjective(BattleSideEnum side, LocalizedString name, LocalizedString desc, bool instantWin, bool requiredForWin) :
			base(side, name, desc, instantWin, requiredForWin)
		{
			_enemiesLeft = -1;
		}

		public KillAllObjective() { }

		public override void RegisterForUpdate()
		{
		}

		public override void UnregisterForUpdate()
		{
		}

		public void UpdateEnemiesLeft(int enemiesLeft)
		{
			_enemiesLeft = enemiesLeft;
		}

		public override bool CheckObjective()
		{
			if (Mission.Current.AttackerTeam == null || Mission.Current.DefenderTeam == null) return false;

			if (Side == BattleSideEnum.Defender)
			{
				_enemiesLeft = Mission.Current.AttackerTeam.ActiveAgents.Count;
			}
			else
			{
				_enemiesLeft = Mission.Current.DefenderTeam.ActiveAgents.Count;
			}
			if (_enemiesLeft == 0) return true;
			return false;
		}

		public override void Reset()
		{
			Active = true;
		}

		public override string GetProgressAsString()
		{
			return _enemiesLeft != -1 ? _enemiesLeft + " enemies left." : "";
		}
	}
}
