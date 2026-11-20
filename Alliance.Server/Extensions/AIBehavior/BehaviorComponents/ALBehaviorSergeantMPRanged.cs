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
			_flagpositions = Formation.Team.Mission.ActiveMissionObjects.FindAllWithType<FlagCapturePoint>().ToList();
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
						float num2 = formation2.CachedMedianPosition.AsVec2.DistanceSquared(Formation.CachedAveragePosition);
						if (num2 < num)
						{
							num = num2;
							formation = formation2;
						}
					}
				}
			}

			if (Formation.Team.FormationsIncludingEmpty.AnyQ((Formation f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation))
			{
				_attachedInfantry = TaleWorlds.Core.Extensions.MinBy(Formation.Team.FormationsIncludingEmpty.Where((Formation f) => f.CountOfUnits > 0 && f != Formation && f.QuerySystem.IsInfantryFormation), (Formation f) => f.CachedMedianPosition.AsVec2.DistanceSquared(Formation.CachedAveragePosition));
				Formation formation3 = null;
				if (flag)
				{
					if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.DistanceSquared(Formation.CachedAveragePosition) <= 4900f)
					{
						formation3 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation;
					}
					else if (formation != null)
					{
						formation3 = formation;
					}
				}

				Vec2 vec = ((formation3 == null) ? _attachedInfantry.Direction : (formation3.CachedMedianPosition.AsVec2 - _attachedInfantry.CachedMedianPosition.AsVec2).Normalized());
				WorldPosition cachedMedianPosition = _attachedInfantry.CachedMedianPosition;
				cachedMedianPosition.SetVec2(cachedMedianPosition.AsVec2 - vec * ((_attachedInfantry.Depth + Formation.Depth) / 2f));
				CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition);
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec);
			}
			else if (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null && Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.DistanceSquared(Formation.CachedAveragePosition) <= 4900f)
			{
				Vec2 vec2 = (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - Formation.CachedAveragePosition).Normalized();
				float num3 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2.Distance(Formation.CachedAveragePosition);
				WorldPosition cachedMedianPosition2 = Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition;
				if (num3 > Formation.QuerySystem.MissileRangeAdjusted)
				{
					cachedMedianPosition2.SetVec2(cachedMedianPosition2.AsVec2 - vec2 * (Formation.QuerySystem.MissileRangeAdjusted - Formation.Depth * 0.5f));
				}
				else if (num3 < Formation.QuerySystem.MissileRangeAdjusted * 0.4f)
				{
					cachedMedianPosition2.SetVec2(cachedMedianPosition2.AsVec2 - vec2 * (Formation.QuerySystem.MissileRangeAdjusted * 0.4f));
				}
				else
				{
					cachedMedianPosition2.SetVec2(Formation.CachedAveragePosition);
				}

				CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition2);
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(vec2);
			}
			else if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team))
			{
				Vec3 position = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) != Formation.Team), (FlagCapturePoint fp) => fp.Position.AsVec2.DistanceSquared(Formation.CachedAveragePosition)).Position;
				if (CurrentOrder.OrderEnum == MovementOrder.MovementOrderEnum.Invalid || CurrentOrder.GetPosition(Formation) != position.AsVec2)
				{
					Vec2 direction = ((Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation != null) ? (Formation.QuerySystem.ClosestSignificantlyLargeEnemyFormation.Formation.CachedMedianPosition.AsVec2 - Formation.CachedAveragePosition).Normalized() : Formation.Direction);
					WorldPosition position2 = new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, position, hasValidZ: false);
					CurrentOrder = MovementOrder.MovementOrderMove(position2);
					CurrentFacingOrder = FacingOrder.FacingOrderLookAtDirection(direction);
				}
			}
			else if (_flagpositions.Any((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team))
			{
				Vec3 position3 = TaleWorlds.Core.Extensions.MinBy(_flagpositions.Where((FlagCapturePoint fp) => _flagDominationGameMode.GetFlagOwnerTeam(fp) == Formation.Team), (FlagCapturePoint fp) => fp.Position.AsVec2.DistanceSquared(Formation.CachedAveragePosition)).Position;
				CurrentOrder = MovementOrder.MovementOrderMove(new WorldPosition(Formation.Team.Mission.Scene, UIntPtr.Zero, position3, hasValidZ: false));
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			}
			else
			{
				WorldPosition cachedMedianPosition3 = Formation.CachedMedianPosition;
				cachedMedianPosition3.SetVec2(Formation.CachedAveragePosition);
				CurrentOrder = MovementOrder.MovementOrderMove(cachedMedianPosition3);
				CurrentFacingOrder = FacingOrder.FacingOrderLookAtEnemy;
			}
		}

		public override void TickOccasionally()
		{
			_flagpositions.RemoveAll((FlagCapturePoint fp) => fp.IsDeactivated);
			CalculateCurrentOrder();
			Formation.SetMovementOrder(CurrentOrder);
			Formation.SetFacingOrder(CurrentFacingOrder);
		}

		protected override void OnBehaviorActivatedAux()
		{
			CalculateCurrentOrder();
			Formation.SetMovementOrder(CurrentOrder);
			Formation.SetFacingOrder(CurrentFacingOrder);
			Formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLoose);
			Formation.SetFiringOrder(FiringOrder.FiringOrderFireAtWill);
			Formation.SetFormOrder(FormOrder.FormOrderWide);
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
