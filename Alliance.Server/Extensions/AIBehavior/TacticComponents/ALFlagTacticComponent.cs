using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior.TacticComponents
{
	public class ALFlagTacticComponent : TacticComponent
	{
		public ALFlagTacticComponent(Team team) : base(team)
		{
		}

		public override void TickOccasionally()
		{
			foreach (Formation item in FormationsIncludingEmpty)
			{
				if (item.CountOfUnits > 0)
				{
					item.AI.ResetBehaviorWeights();
					item.AI.SetBehaviorWeight<BehaviorCharge>(1f);
					item.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);

					item.AI.SetBehaviorWeight<BehaviorSergeantMPInfantry>(1f);
					item.AI.SetBehaviorWeight<BehaviorSergeantMPRanged>(1f);
					item.AI.SetBehaviorWeight<BehaviorSergeantMPMounted>(1f);
					item.AI.SetBehaviorWeight<BehaviorSergeantMPMountedRanged>(1f);
					item.AI.SetBehaviorWeight<BehaviorSergeantMPLastFlagLastStand>(1f);
				}
			}
		}
	}
}
