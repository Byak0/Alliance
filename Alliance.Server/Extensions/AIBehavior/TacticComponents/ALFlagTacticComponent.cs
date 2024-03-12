using Alliance.Server.Extensions.AIBehavior.BehaviorComponents;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.TacticComponents
{
    public class ALFlagTacticComponent : TacticComponent
    {
        public ALFlagTacticComponent(Team team) : base(team)
        {
        }

        protected override void TickOccasionally()
        {
            foreach (Formation item in FormationsIncludingEmpty)
            {
                if (item.CountOfUnits > 0)
                {
                    item.AI.ResetBehaviorWeights();
                    item.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                    item.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorSergeantMPInfantry>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorSergeantMPRanged>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorSergeantMPMounted>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorSergeantMPMountedRanged>(1f);
                    item.AI.SetBehaviorWeight<ALBehaviorSergeantMPLastFlagLastStand>(1f);
                }
            }
        }
    }
}
