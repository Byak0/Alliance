using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.GameModes.Story.Behaviors
{
    /// <summary>
    /// Behavior keeping track of useful informations for objectives and sending events on update.
    /// </summary>
    public class ObjectivesBehavior : MissionBehavior
    {
        public event Action<int> UpdateTotalDefendersKilled;
        public event Action<int> UpdateTotalAttackersKilled;

        public ScenarioManager ScenarioManager { get; private set; }
        public bool ObjectiveIsOver => _objectiveIsOver;
        public MissionTimer MissionTimer => _missionTimer;
        public bool IsTimerRunning { get; private set; }

        public int TotalAttackerDead { get; set; } = 0;
        public int TotalDefenderDead { get; set; } = 0;

        private MissionTimer _missionTimer;
        private float _objectiveTick;
        private bool _objectiveIsOver;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        public ObjectivesBehavior(ScenarioManager scenarioManager) : base()
        {
            ScenarioManager = scenarioManager;
        }

        public override void OnBehaviorInitialize()
        {
            ScenarioManager.OnActStateInProgress += Register;
            ScenarioManager.OnActStateDisplayResults += Unregister;
        }

        public override void OnRemoveBehavior()
        {
            ScenarioManager.OnActStateInProgress -= Register;
            ScenarioManager.OnActStateDisplayResults -= Unregister;
        }

        public override void OnMissionTick(float dt)
        {
            if (!_objectiveIsOver && ScenarioManager.ActState == ActState.InProgress)
            {
                _objectiveTick += dt;
                if (_objectiveTick > 1f)
                {
                    _objectiveTick = 0;
                    _objectiveIsOver = ScenarioManager.CheckObjectives();
                }
            }
        }

        public override void OnAgentRemoved(Agent victim, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (victim?.Team != null)
            {
                if (victim.Team.IsDefender)
                {
                    TotalDefenderDead++;
                    UpdateTotalDefendersKilled?.Invoke(TotalDefenderDead);
                }
                else
                {
                    TotalAttackerDead++;
                    UpdateTotalAttackersKilled?.Invoke(TotalAttackerDead);
                }
            }
        }

        public void Reset()
        {
            TotalAttackerDead = 0;
            TotalDefenderDead = 0;
            _objectiveIsOver = false;
        }

        public int GetNumberOfEnemyKilled(BattleSideEnum side)
        {
            return side == BattleSideEnum.Defender ? TotalAttackerDead : TotalDefenderDead;
        }

        private void Register()
        {
            Reset();
            // Register objectives to start updates
            ScenarioManager.RegisterObjectives();
        }

        private void Unregister()
        {
            Log("ObjectivesBehavior - Unregister", LogLevel.Debug);
            // If result isn't calculated yet, check objectives one last time to get it
            if (!_objectiveIsOver) _objectiveIsOver = ScenarioManager.CheckObjectives();
            // Unregister objectives to stop updates
            ScenarioManager.UnregisterObjectives();
        }

        public void StartTimerAsServer(float duration)
        {
            _missionTimer = new MissionTimer(duration);
            IsTimerRunning = true;
        }

        public void StartTimerAsClient(float startTime, float duration)
        {
            _missionTimer = MissionTimer.CreateSynchedTimerClient(startTime, duration);
            IsTimerRunning = true;
        }

        public float GetRemainingTime(bool isSynched)
        {
            if (!IsTimerRunning)
            {
                return 0f;
            }

            float remainingTimeInSeconds = _missionTimer.GetRemainingTimeInSeconds(isSynched);
            if (isSynched)
            {
                return MathF.Min(remainingTimeInSeconds, _missionTimer.GetTimerDuration());
            }

            return remainingTimeInSeconds;
        }

        public bool HasTimerElapsed()
        {
            if (IsTimerRunning)
            {
                return _missionTimer.Check();
            }
            return false;
        }

        public MissionTime GetCurrentTimerStartTime()
        {
            return _missionTimer.GetStartTime();
        }
    }
}