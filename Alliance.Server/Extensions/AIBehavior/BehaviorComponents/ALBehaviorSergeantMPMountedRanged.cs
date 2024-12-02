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
            _flagpositions = Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>()?.ToList() ?? new List<FlagCapturePoint>();
            _flagDominationGameMode = Formation.Team.Mission.GetMissionBehavior<ALMissionMultiplayerFlagDomination>();
            CalculateCurrentOrder();
        }

        private MovementOrder UncapturedFlagMoveOrder()
        {
            if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team))
            {
                FlagCapturePoint flagCapturePoint = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team), (fp) => Formation.Team.QuerySystem.GetLocalEnemyPower(fp.Position.AsVec2));
                return MovementOrder.MovementOrderMove(new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, flagCapturePoint.Position, hasValidZ: false));
            }

            if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team))
            {
                Vec3 position = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team), (fp) => fp.Position.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition)).Position;
                return MovementOrder.MovementOrderMove(new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, position, hasValidZ: false));
            }

            return MovementOrder.MovementOrderStop;
        }

        protected override void CalculateCurrentOrder()
        {
            if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation == null || Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) > 2500f)
            {
                CurrentOrder = UncapturedFlagMoveOrder();
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
                return;
            }

            FlagCapturePoint flagCapturePoint = null;
            if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team && !fp.IsContested))
            {
                flagCapturePoint = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team && !fp.IsContested), (fp) => Formation.QuerySystem.AveragePosition.DistanceSquared(fp.Position.AsVec2));
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

            Vec2 vec = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition;
            float num = vec.Normalize();
            WorldPosition medianPosition = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition;
            if (num > Formation.QuerySystem.MissileRangeAdjusted)
            {
                medianPosition.SetVec2(medianPosition.AsVec2 - vec * (Formation.QuerySystem.MissileRangeAdjusted - Formation.Depth * 0.5f));
            }
            else if (num < Formation.QuerySystem.MissileRangeAdjusted * 0.4f)
            {
                medianPosition.SetVec2(medianPosition.AsVec2 - vec * (Formation.QuerySystem.MissileRangeAdjusted * 0.7f));
            }
            else
            {
                vec = vec.RightVec();
                medianPosition.SetVec2(Formation.QuerySystem.AveragePosition + vec * 20f);
            }

            CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
            CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
        }

        public override void TickOccasionally()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            if (CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.ChargeToTarget && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation)
            {
                Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
            }
            else
            {
                Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            }
        }

        protected override void OnBehaviorActivatedAux()
        {
            _flagpositions.RemoveAll((fp) => fp.IsDeactivated);
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderDeep;
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
