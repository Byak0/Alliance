using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Models.Objectives
{
	public class KillAllObjective : ObjectiveBase
	{
		public int EnemiesLeft { get; set; }

		public KillAllObjective(BattleSideEnum side, string name, string desc, bool instantWin, bool requiredForWin) :
			base(side, name, desc, instantWin, requiredForWin)
		{
			EnemiesLeft = -1;
		}

		public override void RegisterForUpdate()
		{
		}

		public override void UnregisterForUpdate()
		{
		}

		public void UpdateEnemiesLeft(int enemiesLeft)
		{
			EnemiesLeft = enemiesLeft;
		}

		public override bool CheckObjective()
		{
			if (Mission.Current.AttackerTeam == null || Mission.Current.DefenderTeam == null) return false;

			if (Side == BattleSideEnum.Defender)
			{
				EnemiesLeft = Mission.Current.AttackerTeam.ActiveAgents.Count;
			}
			else
			{
				EnemiesLeft = Mission.Current.DefenderTeam.ActiveAgents.Count;
			}
			if (EnemiesLeft == 0) return true;
			return false;
		}

		public override void Reset()
		{
			Active = true;
		}

		public override string GetProgressAsString()
		{
			return EnemiesLeft != -1 ? EnemiesLeft + " enemies left." : "";
		}
	}
}
