using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.TacticComponents
{
    public class SimpleTacticComponent : TacticComponent
    {
        public SimpleTacticComponent(Team team) : base(team)
        {
        }

        protected override void TickOccasionally()
        {

            foreach (Formation item in FormationsIncludingEmpty)
            {
                if (item.CountOfUnits > 0)
                {
                    item.AI.ResetBehaviorWeights();
                }
            }
        }
    }
}
