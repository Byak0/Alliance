using Alliance.Server.GameModes.CaptainX.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
    public class ALBehaviorSergeantMPLastFlagLastStand : BehaviorComponent
    {
        private List<FlagCapturePoint> _flagpositions;

        private bool _lastEffort;

        private ALMissionMultiplayerFlagDomination _flagDominationGameMode;

        public ALBehaviorSergeantMPLastFlagLastStand(Formation formation)
            : base(formation)
        {
            _flagpositions = Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>()?.ToList() ?? new List<FlagCapturePoint>();
            _flagDominationGameMode = Mission.Current.GetMissionBehavior<ALMissionMultiplayerFlagDomination>();
            CalculateCurrentOrder();
        }

        protected override void CalculateCurrentOrder()
        {
            CurrentOrder = _flagpositions.Count > 0 ? MovementOrder.MovementOrderMove(new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, _flagpositions[0].Position, hasValidZ: false)) : MovementOrder.MovementOrderStop;
        }

        public override void TickOccasionally()
        {
            _flagpositions.RemoveAll((fp) => fp.IsDeactivated);
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
        }

        protected override void OnBehaviorActivatedAux()
        {
            _flagpositions.RemoveAll((fp) => fp.IsDeactivated);
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
            Formation.FacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            Formation.FiringOrder = FiringOrder.FiringOrderHoldYourFire;
            Formation.FormOrder = FormOrder.FormOrderDeep;
        }

        protected override float GetAiWeight()
        {
            if (_lastEffort)
            {
                return 10f;
            }

            _flagpositions.RemoveAll((fp) => fp.IsDeactivated);
            FlagCapturePoint flagCapturePoint = _flagpositions.FirstOrDefault();
            if (_flagpositions.Count != 1 || _flagDominationGameMode.GetFlagOwnerTeam(flagCapturePoint) == null || !_flagDominationGameMode.GetFlagOwnerTeam(flagCapturePoint).IsEnemyOf(Formation.Team))
            {
                return 0f;
            }

            float timeUntilBattleSideVictory = _flagDominationGameMode.GetTimeUntilBattleSideVictory(_flagDominationGameMode.GetFlagOwnerTeam(flagCapturePoint).Side);
            if (timeUntilBattleSideVictory <= 60f)
            {
                return 10f;
            }

            float num = Formation.QuerySystem.AveragePosition.Distance(flagCapturePoint.Position.AsVec2);
            float movementSpeedMaximum = Formation.QuerySystem.MovementSpeedMaximum;
            if (num / movementSpeedMaximum * 8f > timeUntilBattleSideVictory)
            {
                _lastEffort = true;
                return 10f;
            }

            return 0f;
        }
    }
}
