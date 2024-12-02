using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.Utils
{
    /// <summary>
    /// Update TeamQuerySystem with correct power ratio.
    /// Credits to BattleLink :)
    /// </summary>
    public class TeamQuerySystemUtils
    {
        private static readonly ALBattlePowerCalculationLogic battlePowerLogic = new ALBattlePowerCalculationLogic();
        private static readonly FieldInfo fTeamRemainingPowerRatio = typeof(TeamQuerySystem).GetField("_remainingPowerRatio", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo fTeamTotalPowerRatio = typeof(TeamQuerySystem).GetField("_totalPowerRatio", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void SetMission(Mission mission)
        {
            battlePowerLogic.SetMission(mission);
        }

        public static void SetPowerFix(Mission mission)
        {
            battlePowerLogic.SetMission(mission);
            SetPower(mission.AttackerTeam, mission.DefenderTeam);
            SetPower(mission.DefenderTeam, mission.AttackerTeam);
        }

        private static void SetPower(Team team, Team teamEn)
        {
            QueryData<float> qdRemainingPowerRatio = new QueryData<float>(() =>
            {
                float res = (float)(((double)MathF.Max(0.0f, battlePowerLogic.GetTotalTeamPower(team) - team.FormationsIncludingSpecialAndEmpty.Sum(f => team.QuerySystem.CasualtyHandler.GetCasualtyPowerLossOfFormation(f))) + 1.0)
                                / ((double)MathF.Max(0.0f, battlePowerLogic.GetTotalTeamPower(teamEn) - teamEn.FormationsIncludingSpecialAndEmpty.Sum(f => team.QuerySystem.CasualtyHandler.GetCasualtyPowerLossOfFormation(f))) + 1.0));
                return res;
            }, 5f);
            fTeamRemainingPowerRatio.SetValue(team.QuerySystem, qdRemainingPowerRatio);

            QueryData<float> qdTotalPowerRatio = new QueryData<float>(() =>
            {
                float res = (float)(((double)battlePowerLogic.GetTotalTeamPower(team) + 1.0) / ((double)battlePowerLogic.GetTotalTeamPower(teamEn) + 1.0));
                return res;
            }, 10f);
            fTeamTotalPowerRatio.SetValue(team.QuerySystem, qdTotalPowerRatio);
        }
    }
}
