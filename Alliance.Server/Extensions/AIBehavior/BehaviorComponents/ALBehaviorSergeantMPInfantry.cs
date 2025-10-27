namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
	//public class ALBehaviorSergeantMPInfantry : BehaviorComponent
	//{
	//	private enum BehaviorState
	//	{
	//		GoingToFlag,
	//		Attacking,
	//		Unset
	//	}

	//	private BehaviorState _behaviorState;

	//	private List<FlagCapturePoint> _flagpositions;

	//	private ALMissionMultiplayerFlagDomination _flagDominationGameMode;

	//	public ALBehaviorSergeantMPInfantry(Formation formation)
	//		: base(formation)
	//	{
	//		_behaviorState = BehaviorState.Unset;
	//		_flagpositions = Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>()?.ToList() ?? new List<FlagCapturePoint>();
	//		_flagDominationGameMode = Formation.Team.Mission.GetMissionBehavior<ALMissionMultiplayerFlagDomination>();
	//		CalculateCurrentOrder();
	//	}

	//	protected override void CalculateCurrentOrder()
	//	{
	//		BehaviorState behaviorState = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation && Formation.QuerySystem.AverageAllyPosition.DistanceSquared(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.AverageAllyPosition) <= (_behaviorState == BehaviorState.Attacking ? 3600f : 2500f) || Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsInfantryFormation && Formation.QuerySystem.AverageAllyPosition.DistanceSquared(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.AverageAllyPosition) <= (_behaviorState == BehaviorState.Attacking ? 900f : 400f)) ? BehaviorState.Attacking : BehaviorState.GoingToFlag;
	//		if (behaviorState == BehaviorState.Attacking && (_behaviorState != BehaviorState.Attacking || CurrentOrder.OrderEnum != MovementOrder.MovementOrderEnum.ChargeToTarget || CurrentOrder.TargetFormation.QuerySystem != Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation))
	//		{
	//			_behaviorState = BehaviorState.Attacking;
	//			CurrentOrder = MovementOrder.MovementOrderChargeToTarget(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation);
	//		}

	//		if (behaviorState != 0)
	//		{
	//			return;
	//		}

	//		_behaviorState = behaviorState;
	//		WorldPosition position = new WorldPosition();
	//		if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team))
	//		{
	//			position = new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team), (fp) => fp.Position.AsVec2.DistanceSquared(Formation.QuerySystem.AverageAllyPosition)).Position, hasValidZ: false);
	//		}
	//		else if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team))
	//		{
	//			position = new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team), (fp) => fp.Position.AsVec2.DistanceSquared(Formation.QuerySystem.AverageAllyPosition)).Position, hasValidZ: false);
	//		}
	//		else
	//		{
	//			position.SetVec2(Formation.QuerySystem.AverageAllyPosition);
	//		}

	//		if (CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.Invalid || CurrentOrder.GetPosition(Formation) != position.AsVec2)
	//		{
	//			Vec2 direction = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null ? (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.AverageAllyPosition - Formation.QuerySystem.AverageAllyPosition).Normalized() : Formation.Direction;
	//			CurrentOrder = MovementOrder.MovementOrderMove(position);
	//			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
	//		}
	//	}

	//	public override void TickOccasionally()
	//	{
	//		_flagpositions.RemoveAll((fp) => fp.IsDeactivated);
	//		CalculateCurrentOrder();
	//		Formation.SetMovementOrder(CurrentOrder);
	//		Formation.SetFacingOrder(CurrentFacingOrder);
	//		if (Formation.QuerySystem.HasShield && (_behaviorState == BehaviorState.Attacking || _behaviorState == BehaviorState.GoingToFlag && CurrentOrder.GetPosition(Formation).IsValid && Formation.QuerySystem.AverageAllyPosition.DistanceSquared(CurrentOrder.GetPosition(Formation)) <= 225f))
	//		{
	//			Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderShieldWall);
	//		}
	//		else
	//		{
	//			Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
	//		}
	//	}

	//	protected override void OnBehaviorActivatedAux()
	//	{
	//		CalculateCurrentOrder();
	//		Formation.SetMovementOrder(CurrentOrder);
	//		Formation.SetFacingOrder(CurrentFacingOrder);
	//		Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
	//		Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
	//		Formation.SetFormOrder(FormOrder.FormOrderDeep);
	//	}

	//	protected override float GetAiWeight()
	//	{
	//		if (Formation.QuerySystem.IsInfantryFormation)
	//		{
	//			return 1.2f;
	//		}

	//		return 0f;
	//	}
	//}
}
