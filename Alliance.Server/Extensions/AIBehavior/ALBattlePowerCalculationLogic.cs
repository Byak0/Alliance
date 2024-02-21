using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior
{
    public class ALBattlePowerCalculationLogic
    {
        public bool IsTeamPowersCalculated { get; private set; }

        public Mission Mission { get; set; }

        public void SetMission(Mission mission)
        {
            Mission = mission;
            for (int i = 0; i < 2; i++)
            {
                _sidePowerData[i].Clear();
            }
            IsTeamPowersCalculated = false;
        }

        public ALBattlePowerCalculationLogic()
        {
            _sidePowerData = new Dictionary<Team, float>[2];
            for (int i = 0; i < 2; i++)
            {
                _sidePowerData[i] = new Dictionary<Team, float>();
            }
            IsTeamPowersCalculated = false;
        }

        public float GetTotalTeamPower(Team team)
        {
            if (!IsTeamPowersCalculated)
            {
                CalculateTeamPowers();
            }
            return _sidePowerData[(int)team.Side][team];
        }

        private void CalculateTeamPowers()
        {
            List<Team> list = Enumerable.ToList<Team>(Enumerable.Where<Team>(Mission.Teams, (Team t) => t.Side != BattleSideEnum.None));
            foreach (Team team in list)
            {
                _sidePowerData[(int)team.Side].Add(team, 0f);
            }
            foreach (Team team2 in list)
            {
                Dictionary<Team, float> dictionary = _sidePowerData[(int)team2.Side];
                foreach (Agent agent in team2.ActiveAgents)
                {
                    Dictionary<Team, float> dictionary2 = dictionary;
                    Team team3 = team2;
                    dictionary2[team3] += agent.CharacterPowerCached;
                }
            }
            foreach (Team team4 in list)
            {
                team4.QuerySystem.Expire();
            }
            IsTeamPowersCalculated = true;
        }

        private Dictionary<Team, float>[] _sidePowerData;
    }
}
