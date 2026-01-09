using Alliance.Server.GameModes.CaptainX.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
	public class ALBehaviorSergeantMPMountedRanged : BehaviorComponent
	{
		private List<FlagCapturePoint> _flagpositions;

		private ALMissionMultiplayerFlagDomination _flagDominationGameMode;

		public ALBehaviorSergeantMPMountedRanged(Formation formation)
			: base(formation)
		{
			_flagpositions = Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>().ToList();
			_flagDominationGameMode = Formation.Team.Mission.GetMissionBehavior<ALMissionMultiplayerFlagDomination>();
			CalculateCurrentOrder();
		}

		private MovementOrder UncapturedFlagMoveOrder()
		{
			if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team))
			{
				FlagCapturePoint flagCapturePoint = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team), (FlagCapturePoint fp) => Formation.Team.QuerySystem.GetLocalEnemyPower(fp.Position.AsVec2));
				return MovementOrder.MovementOrderMove(new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, flagCapturePoint.Position, hasValidZ: false));
			}

			if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team))
			{
				Vec3 position = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team), (FlagCapturePoint fp) => fp.Position.AsVec2.DistanceSquared(Formation.CachedAveragePosition)).Position;
				return MovementOrder.MovementOrderMove(new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, position, hasValidZ: false));
			}

			return MovementOrder.MovementOrderStop;
		}

		protected override void CalculateCurrentOrder()
		{
			if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null || Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.DistanceSquared(Formation.CachedAveragePosition) > 2500f)
			{
				CurrentOrder = UncapturedFlagMoveOrder();
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
				return;
			}

			FlagCapturePoint flagCapturePoint = null;
			if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team && !fp.IsContested))
			{
				flagCapturePoint = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team && !fp.IsContested), (FlagCapturePoint fp) => Formation.CachedAveragePosition.DistanceSquared(fp.Position.AsVec2));
			}

			if (!Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsInfantryFormation && flagCapturePoint != null)
			{
				WorldPosition position = new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, flagCapturePoint.Position, hasValidZ: false);
				CurrentOrder = MovementOrder.MovementOrderMove(position);
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
				return;
			}

			if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation)
			{
				CurrentOrder = MovementOrder.MovementOrderChargeToTarget(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation);
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
				return;
			}

			Vec2 vec = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - Formation.CachedAveragePosition;
			float num = vec.Normalize();
			WorldPosition cachedMedianPosition = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition;
			if (num > Formation.QuerySystem.MissileRangeAdjusted)
			{
				cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec * (Formation.QuerySystem.MissileRangeAdjusted - Formation.Depth * 0.5f));
			}
			else if (num < Formation.QuerySystem.MissileRangeAdjusted * 0.4f)
			{
				cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec * (Formation.QuerySystem.MissileRangeAdjusted * 0.7f));
			}
			else
			{
				vec = vec.RightVec();
				cachedMedianPosition.SetVec2(Formation.CachedAveragePosition + vec * 20f);
			}

			CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
			CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
		}

		public override void TickOccasionally()
		{
			CalculateCurrentOrder();
			Formation.SetMovementOrder(CurrentOrder);
			Formation.SetFacingOrder(CurrentFacingOrder);
			if (CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation)
			{
				Formation.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
			}
			else
			{
				Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
			}
		}

		protected override void OnBehaviorActivatedAux()
		{
			_flagpositions.RemoveAll((FlagCapturePoint fp) => fp.IsDeactivated);
			CalculateCurrentOrder();
			Formation.SetMovementOrder(CurrentOrder);
			Formation.SetFacingOrder(CurrentFacingOrder);
			Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
			Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
			Formation.SetFormOrder(FormOrder.FormOrderDeep);
		}

		protected override float GetAiWeight()
		{
			if (Formation.QuerySystem.IsRangedCavalryFormation)
			{
				return 1.2f;
			}

			return 0f;
		}
	}
}
