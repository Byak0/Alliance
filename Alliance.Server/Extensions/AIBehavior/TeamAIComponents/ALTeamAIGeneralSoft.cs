using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.TeamAIComponents
{
    public class ALTeamAIGeneralSoft : TeamAIComponent
    {
        public ALTeamAIGeneralSoft(Mission currentMission, Team currentTeam, float thinkTimerTime = 10f, float applyTimerTime = 1f)
            : base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
        {
        }

        public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
        {
        }

        private void UpdateVariables()
        {
            TeamQuerySystem querySystem = Team.QuerySystem;
            Vec2 averagePosition = querySystem.AveragePosition;
            foreach (Agent agent in Mission.Agents)
            {
                if (!agent.IsMount && agent.Team.IsValid && agent.Team.IsEnemyOf(Team))
                {
                    float num = agent.Position.DistanceSquared(new Vec3(averagePosition.x, averagePosition.y));
                }
            }
        }
    }
}
