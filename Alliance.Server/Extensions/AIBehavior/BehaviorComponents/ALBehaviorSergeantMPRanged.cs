using Alliance.Server.GameModes.CaptainX.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace Alliance.Server.Extensions.AIBehavior.BehaviorComponents
{
    public class ALBehaviorSergeantMPRanged : BehaviorComponent
    {
        private List<FlagCapturePoint> _flagpositions;

        private Formation _attachedInfantry;

        private ALMissionMultiplayerFlagDomination _flagDominationGameMode;

        public ALBehaviorSergeantMPRanged(Formation formation)
            : base(formation)
        {
            _flagpositions = Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>()?.ToList() ?? new List<FlagCapturePoint>();
            _flagDominationGameMode = Formation.Team.Mission.GetMissionBehavior<ALMissionMultiplayerFlagDomination>();
            CalculateCurrentOrder();
        }

        protected override void CalculateCurrentOrder()
        {
            bool flag = false;
            Formation formation = null;
            float num = float.MaxValue;
            foreach (Team team in Formation.Team.Mission.Teams)
            {
                if (!team.IsEnemyOf(Formation.Team))
                {
                    continue;
                }

                for (int i = 0; i < Math.Min(team.FormationsIncludingSpecialAndEmpty.Count, 8); i++)
                {
                    Formation formation2 = team.FormationsIncludingSpecialAndEmpty[i];
                    if (formation2.CountOfUnits <= 0)
                    {
                        continue;
                    }

                    flag = true;
                    if (formation2.QuerySystem.IsCavalryFormation || formation2.QuerySystem.IsRangedCavalryFormation)
                    {
                        float num2 = formation2.QuerySystem.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition);
                        if (num2 < num)
                        {
                            num = num2;
                            formation = formation2;
                        }
                    }
                }
            }

            if (Formation.Team.FormationsIncludingEmpty.AnyQ((f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation))
            {
                _attachedInfantry = TaleWorlds.Core.Extensions.MinBy(Formation.Team.FormationsIncludingEmpty.Where((f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation), (f) => f.QuerySystem.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition));
                Formation formation3 = null;
                if (flag)
                {
                    if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) <= 4900f)
                    {
                        formation3 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
                    }
                    else if (formation != null)
                    {
                        formation3 = formation;
                    }
                }

                Vec2 vec = formation3 == null ? _attachedInfantry.Direction : (formation3.QuerySystem.MedianPosition.AsVec2 - _attachedInfantry.QuerySystem.MedianPosition.AsVec2).Normalized();
                WorldPosition medianPosition = _attachedInfantry.QuerySystem.MedianPosition;
                medianPosition.SetVec2(medianPosition.AsVec2 - vec * ((_attachedInfantry.Depth + Formation.Depth) / 2f));
                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
            }
            else if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition) <= 4900f)
            {
                Vec2 vec2 = (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition).Normalized();
                float num3 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2.Distance(Formation.QuerySystem.AveragePosition);
                WorldPosition medianPosition2 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition;
                if (num3 > Formation.QuerySystem.MissileRangeAdjusted)
                {
                    medianPosition2.SetVec2(medianPosition2.AsVec2 - vec2 * (Formation.QuerySystem.MissileRangeAdjusted - Formation.Depth * 0.5f));
                }
                else if (num3 < Formation.QuerySystem.MissileRangeAdjusted * 0.4f)
                {
                    medianPosition2.SetVec2(medianPosition2.AsVec2 - vec2 * (Formation.QuerySystem.MissileRangeAdjusted * 0.4f));
                }
                else
                {
                    medianPosition2.SetVec2(Formation.QuerySystem.AveragePosition);
                }

                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition2);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec2);
            }
            else if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team))
            {
                Vec3 position = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team), (fp) => fp.Position.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition)).Position;
                if (CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.Invalid || CurrentOrder.GetPosition(Formation) != position.AsVec2)
                {
                    Vec2 direction = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null ? (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.MedianPosition.AsVec2 - Formation.QuerySystem.AveragePosition).Normalized() : Formation.Direction;
                    WorldPosition position2 = new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, position, hasValidZ: false);
                    CurrentOrder = MovementOrder.MovementOrderMove(position2);
                    CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
                }
            }
            else if (_flagpositions.Any((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team))
            {
                Vec3 position3 = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team), (fp) => fp.Position.AsVec2.DistanceSquared(Formation.QuerySystem.AveragePosition)).Position;
                CurrentOrder = MovementOrder.MovementOrderMove(new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, position3, hasValidZ: false));
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            }
            else
            {
                WorldPosition medianPosition3 = Formation.QuerySystem.MedianPosition;
                medianPosition3.SetVec2(Formation.QuerySystem.AveragePosition);
                CurrentOrder = MovementOrder.MovementOrderMove(medianPosition3);
                CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
            }
        }

        public override void TickOccasionally()
        {
            _flagpositions.RemoveAll((fp) => fp.IsDeactivated);
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
        }

        protected override void OnBehaviorActivatedAux()
        {
            CalculateCurrentOrder();
            Formation.SetMovementOrder(CurrentOrder);
            Formation.FacingOrder = CurrentFacingOrder;
            Formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
            Formation.FiringOrder = FiringOrder.FiringOrderFireAtWill;
            Formation.FormOrder = FormOrder.FormOrderWide;
        }

        protected override float GetAiWeight()
        {
            if (Formation.QuerySystem.IsRangedFormation)
            {
                return 1.2f;
            }

            return 0f;
        }
    }
}
