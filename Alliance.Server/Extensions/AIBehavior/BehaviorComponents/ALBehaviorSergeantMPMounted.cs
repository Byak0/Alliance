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
    public class ALBehaviorSergeantMPMounted : BehaviorComponent
    {
        private List<FlagCapturePoint> _flagpositions;

        private ALMissionMultiplayerFlagDomination _flagDominationGameMode;

        public ALBehaviorSergeantMPMounted(Formation formation)
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
                return;
            }

            FlagCapturePoint flagCapturePoint = null;
            if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team && !fp.IsContested))
            {
                flagCapturePoint = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team && !fp.IsContested), (fp) => Formation.QuerySystem.AveragePosition.DistanceSquared(fp.Position.AsVec2));
            }

            if ((!Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.IsRangedFormation || !(Formation.QuerySystem.FormationPower / Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.FormationPower / Formation.Team.QuerySystem.RemainingPowerRatio > 0.7f)) && flagCapturePoint != null)
            {
                CurrentOrder = MovementOrder.MovementOrderMove(new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, flagCapturePoint.Position, hasValidZ: false));
            }
            else
            {
                CurrentOrder = MovementOrder.MovementOrderChargeToTarget(Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation);
            }
        }

        public override void TickOccasionally()
        {
            _flagpositions.RemoveAll((fp) => fp.IsDeactivated);
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            Formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderDeep;
        }

        protected override float GetAiWeight()
        {
            if (Formation.QuerySystem.IsCavalryFormation)
            {
                return 1.2f;
            }

            return 0f;
        }
    }
}
