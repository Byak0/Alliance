using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace Alliance.Common.GameModels
{
    public class ExtendedAutoBlockModel : AutoBlockModel
    {
        public override Agent.UsageDirection GetBlockDirection(Mission mission)
        {
            Agent mainAgent = mission.MainAgent;
            float num = float.MinValue;
            Agent.UsageDirection usageDirection = Agent.UsageDirection.AttackDown;
            foreach (Agent agent in mission.Agents)
            {
                if (agent.IsHuman)
                {
                    Agent.ActionStage currentActionStage = agent.GetCurrentActionStage(1);
                    if ((currentActionStage == Agent.ActionStage.AttackReady || currentActionStage == Agent.ActionStage.AttackQuickReady || currentActionStage == Agent.ActionStage.AttackRelease) && agent.IsEnemyOf(mainAgent))
                    {
                        Vec3 vec = agent.Position - mainAgent.Position;
                        float num2 = vec.Normalize();
                        float num3 = MBMath.ClampFloat(Vec3.DotProduct(vec, mainAgent.LookDirection) + 0.8f, 0f, 1f);
                        float num4 = MBMath.ClampFloat(1f / (num2 + 0.5f), 0f, 1f);
                        float num5 = MBMath.ClampFloat(-Vec3.DotProduct(vec, agent.LookDirection) + 0.5f, 0f, 1f);
                        float num6 = num3 * num4 * num5;
                        if (num6 > num)
                        {
                            num = num6;
                            usageDirection = agent.GetCurrentActionDirection(1);
                            if (usageDirection == Agent.UsageDirection.None)
                            {
                                usageDirection = Agent.UsageDirection.AttackDown;
                            }
                        }
                    }
                }
            }
            return usageDirection;
        }
    }
}
