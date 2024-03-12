using Alliance.Server.Extensions.AIBehavior.BehaviorComponents;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace Alliance.Server.Extensions.AIBehavior.TeamAIComponents
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

                if (Mission.Current.ActiveMissionObjects.Exists(missionObject => missionObject is FlagCapturePoint))
                {
                    formation.AI.AddAiBehavior(new ALBehaviorSergeantMPInfantry(formation));
                    formation.AI.AddAiBehavior(new ALBehaviorSergeantMPLastFlagLastStand(formation));
                    formation.AI.AddAiBehavior(new ALBehaviorSergeantMPMounted(formation));
                    formation.AI.AddAiBehavior(new ALBehaviorSergeantMPMountedRanged(formation));
                    formation.AI.AddAiBehavior(new ALBehaviorSergeantMPRanged(formation));
                }
            }
        }
    }
}
