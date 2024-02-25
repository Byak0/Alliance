using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AIBehavior
{
    /// <summary>
    /// Define AI Behaviors for formations (charge, pullback, etc.).
    /// Credits to BattleLink :)
    /// </summary>
    public class ALTeamAIGeneral : TeamAIGeneral
    {
        private readonly float _occasionalTickTime;
        private MissionTime _nextTacticChooseTime;
        private MissionTime _nextOccasionalTickTime;
        private FieldInfo _availableTacticsFI;
        private PropertyInfo _getIsFirstTacticChosenPI;
        private PropertyInfo _currentTacticPI;
        private PropertyInfo _isDefenseApplicablePI;
        private MethodInfo _getTacticWeightMI;
        private MethodInfo _resetTacticalPositionsMI;

        public FieldInfo AvailableTacticsFI
        {
            get
            {
                if (_availableTacticsFI == null)
                {
                    _availableTacticsFI = typeof(TeamAIComponent).GetField("_availableTactics", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                return _availableTacticsFI;
            }
        }

        public PropertyInfo GetIsFirstTacticChosenPI
        {
            get
            {
                if (_getIsFirstTacticChosenPI == null)
                {
                    _getIsFirstTacticChosenPI = typeof(TeamAIComponent).GetProperty("GetIsFirstTacticChosen", BindingFlags.Instance | BindingFlags.Public);
                }
                return _getIsFirstTacticChosenPI;
            }
        }

        public PropertyInfo CurrentTacticPI
        {
            get
            {
                if (_currentTacticPI == null)
                {
                    _currentTacticPI = typeof(TeamAIComponent).GetProperty("CurrentTactic", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                return _currentTacticPI;
            }
        }

        public PropertyInfo IsDefenseApplicablePI
        {
            get
            {
                if (_isDefenseApplicablePI == null)
                {
                    _isDefenseApplicablePI = typeof(TeamAIComponent).GetProperty("IsDefenseApplicable", BindingFlags.Instance | BindingFlags.Public);
                }
                return _isDefenseApplicablePI;
            }
        }

        public MethodInfo GetTacticWeightMI
        {
            get
            {
                if (_getTacticWeightMI == null)
                {
                    _getTacticWeightMI = typeof(TacticComponent).GetMethod("GetTacticWeight", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return _getTacticWeightMI;
            }
        }

        public MethodInfo ResetTacticalPositionsMI
        {
            get
            {
                if (_resetTacticalPositionsMI == null)
                {
                    _resetTacticalPositionsMI = typeof(TacticComponent).GetMethod("ResetTacticalPositions", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                return _resetTacticalPositionsMI;
            }
        }

        public ALTeamAIGeneral(Mission currentMission, Team currentTeam, float thinkTimerTime = 10f, float applyTimerTime = 1f) : base(currentMission, currentTeam, thinkTimerTime, applyTimerTime)
        {
        }

        public override void OnUnitAddedToFormationForTheFirstTime(Formation formation)
        {
            if (GameNetwork.IsServer && formation.AI.GetBehavior<BehaviorCharge>() == null
                // else bug on position
                && formation.CountOfUnits > 0)
            {
                if (formation.FormationIndex == FormationClass.NumberOfRegularFormations)
                {
                    formation.AI.AddAiBehavior(new BehaviorGeneral(formation));
                }
                else if (formation.FormationIndex == FormationClass.Bodyguard)
                {
                    formation.AI.AddAiBehavior(new BehaviorProtectGeneral(formation));
                }

                formation.AI.AddAiBehavior(new BehaviorCharge(formation));
                formation.AI.AddAiBehavior(new BehaviorPullBack(formation));
                formation.AI.AddAiBehavior(new BehaviorRegroup(formation));
                formation.AI.AddAiBehavior(new BehaviorReserve(formation));
                formation.AI.AddAiBehavior(new BehaviorRetreat(formation));
                formation.AI.AddAiBehavior(new BehaviorStop(formation));
                formation.AI.AddAiBehavior(new BehaviorTacticalCharge(formation));
                formation.AI.AddAiBehavior(new BehaviorAdvance(formation));
                formation.AI.AddAiBehavior(new BehaviorCautiousAdvance(formation));
                formation.AI.AddAiBehavior(new BehaviorCavalryScreen(formation));
                formation.AI.AddAiBehavior(new BehaviorDefend(formation));
                formation.AI.AddAiBehavior(new BehaviorDefensiveRing(formation));
                formation.AI.AddAiBehavior(new BehaviorFireFromInfantryCover(formation));
                formation.AI.AddAiBehavior(new BehaviorFlank(formation));
                formation.AI.AddAiBehavior(new BehaviorHoldHighGround(formation));
                formation.AI.AddAiBehavior(new BehaviorHorseArcherSkirmish(formation));
                formation.AI.AddAiBehavior(new BehaviorMountedSkirmish(formation));
                formation.AI.AddAiBehavior(new BehaviorProtectFlank(formation));
                formation.AI.AddAiBehavior(new BehaviorScreenedSkirmish(formation));
                formation.AI.AddAiBehavior(new BehaviorSkirmish(formation));
                formation.AI.AddAiBehavior(new BehaviorSkirmishBehindFormation(formation));
                formation.AI.AddAiBehavior(new BehaviorSkirmishLine(formation));
                formation.AI.AddAiBehavior(new BehaviorVanguard(formation));

                switch (formation.PhysicalClass)
                {
                    case FormationClass.Infantry:
                        formation.AI.SetBehaviorWeight<BehaviorCharge>(1f);
                        break;
                    case FormationClass.Ranged:
                        formation.AI.SetBehaviorWeight<BehaviorHoldHighGround>(1f);
                        break;
                    case FormationClass.Cavalry:
                        formation.AI.SetBehaviorWeight<BehaviorTacticalCharge>(1f);
                        formation.AI.SetBehaviorWeight<BehaviorFlank>(1f);
                        break;
                    case FormationClass.HorseArcher:
                        formation.AI.SetBehaviorWeight<BehaviorMountedSkirmish>(1f);
                        break;
                    case FormationClass.Skirmisher: break;
                    case FormationClass.HeavyInfantry: break;
                    case FormationClass.LightCavalry: break;
                    case FormationClass.HeavyCavalry: break;
                }
            }
        }

        protected override void Tick(float dt)
        {
            if (Team.BodyGuardFormation != null && Team.BodyGuardFormation.CountOfUnits > 0 && (Team.GeneralsFormation == null || Team.GeneralsFormation.CountOfUnits == 0))
            {
                Team.BodyGuardFormation.AI.ResetBehaviorWeights();
                Team.BodyGuardFormation.AI.SetBehaviorWeight<BehaviorCharge>(1f);
            }
            if (_nextTacticChooseTime.IsPast)
            {
                MakeDecision();
                _nextTacticChooseTime = MissionTime.SecondsFromNow(5f);
            }
            if (_nextOccasionalTickTime.IsPast)
            {
                TickOccasionally();
                _nextOccasionalTickTime = MissionTime.SecondsFromNow(_occasionalTickTime);
            }
        }

        private void MakeDecision()
        {
            List<TacticComponent> availableTactics = (List<TacticComponent>)AvailableTacticsFI.GetValue(this);
            if ((Mission.CurrentState != Mission.State.Continuing && availableTactics.Count == 0) || !Team.HasAnyFormationsIncludingSpecialThatIsNotEmpty())
            {
                return;
            }
            bool flag = true;
            foreach (Team team in Mission.Teams)
            {
                if (team.IsEnemyOf(Team) && team.HasAnyFormationsIncludingSpecialThatIsNotEmpty())
                {
                    flag = false;
                    break;
                }
            }
            if (flag)
            {
                if (Mission.MissionEnded)
                {
                    return;
                }
                if (!(CurrentTactic is TacticCharge))
                {
                    foreach (TacticComponent tacticComponent in availableTactics)
                    {
                        if (tacticComponent is TacticCharge)
                        {
                            if (CurrentTactic == null)
                            {
                                GetIsFirstTacticChosenPI.SetValue(this, true);
                            }
                            CurrentTacticPI.SetValue(this, tacticComponent);
                            break;
                        }
                    }
                    if (!(CurrentTactic is TacticCharge))
                    {
                        if (CurrentTactic == null)
                        {
                            GetIsFirstTacticChosenPI.SetValue(this, true);
                        }
                        CurrentTacticPI.SetValue(this, availableTactics.FirstOrDefault());
                    }
                }
            }
            CheckIsDefenseApplicable();
            MethodInfo GetTacticWeightMI = typeof(TacticComponent).GetMethod("GetTacticWeight", BindingFlags.NonPublic | BindingFlags.Instance);
            TacticComponent tacticComponent2 = TaleWorlds.Core.Extensions.MaxBy(availableTactics, (TacticComponent to) => (float)GetTacticWeightMI.Invoke(to, null) * ((to == CurrentTactic) ? 1.5f : 1f));
            bool flag2 = false;
            if (CurrentTactic == null)
            {
                flag2 = true;
            }
            else if (CurrentTactic != tacticComponent2)
            {
                if (!(bool)ResetTacticalPositionsMI.Invoke(CurrentTactic, null))
                {
                    flag2 = true;
                }
                else
                {
                    float tacticWeight = (float)GetTacticWeightMI.Invoke(tacticComponent2, null);
                    float num = (float)GetTacticWeightMI.Invoke(CurrentTactic, null) * 1.5f;
                    if (tacticWeight > num)
                    {
                        flag2 = true;
                    }
                }
            }
            if (flag2)
            {
                if (CurrentTactic == null)
                {
                    GetIsFirstTacticChosenPI.SetValue(this, true);
                }
                CurrentTacticPI.SetValue(this, tacticComponent2);
                if (Mission.Current.MainAgent != null && Team.GeneralAgent != null && Team.IsPlayerTeam && Team.IsPlayerSergeant)
                {
                    string name = tacticComponent2.GetType().Name;
                    MBInformationManager.AddQuickInformation(GameTexts.FindText("str_team_ai_tactic_text", name), 4000, Team.GeneralAgent.Character, "");
                }
            }
        }

        public new void CheckIsDefenseApplicable()
        {
            // Always set to false to force AI to be aggresive
            IsDefenseApplicablePI.SetValue(this, false);
            return;
        }
    }
}
