using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.GameModes.Story.Behaviors
{
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
            if (ScenarioManager.ActState != ActState.InProgress) return;
            if (_objectiveIsOver) return;

            _objectiveTick += dt;
            if (_objectiveTick < 1f) return;
            _objectiveTick = 0;

            if (GameNetwork.IsServer)
            {
                _objectiveIsOver = ScenarioManager.CheckObjectives();
                SyncObjectiveProgress();
            }
        }

        public override void OnAgentRemoved(Agent victim, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (victim?.Team == null) return;
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

        public void Reset()
        {
            TotalAttackerDead = 0;
            TotalDefenderDead = 0;
            _objectiveIsOver = false;
        }

        private void Register()
        {
            Reset();
            ScenarioManager.RegisterObjectives();
        }

        private void Unregister()
        {
            if (GameNetwork.IsServer && !_objectiveIsOver)
                _objectiveIsOver = ScenarioManager.CheckObjectives();
            ScenarioManager.UnregisterObjectives();
        }

        private void SyncObjectiveProgress()
        {
            if (ScenarioManager.CurrentAct?.Objectives == null) return;

            var objectives = ScenarioManager.CurrentAct.Objectives;
            var data = new SyncObjectiveProgressMessage.ObjectiveData[objectives.Count];
            VariableStore context = ScenarioManager.Globals;
            VariableStore globals = ScenarioManager.Globals;

            for (int i = 0; i < objectives.Count; i++)
            {
                var obj = objectives[i];
                data[i].IsCompleted = obj.IsCompleted;
                data[i].Elements = EvaluateProgressElements(obj, context, globals);
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SyncObjectiveProgressMessage(data));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        private SyncObjectiveProgressMessage.ElementData[] EvaluateProgressElements(
            Objective objective, VariableStore context, VariableStore globals)
        {
            if (objective.Progress == null || objective.Progress.Count == 0)
                return Array.Empty<SyncObjectiveProgressMessage.ElementData>();

            var result = new SyncObjectiveProgressMessage.ElementData[objective.Progress.Count];
            for (int i = 0; i < objective.Progress.Count; i++)
            {
                var el = objective.Progress[i];
                ref var d = ref result[i];

                if (el is TextElement text)
                {
                    d.Type = 0;
                    var values = new object[text.Values?.Count ?? 0];
                    for (int j = 0; j < values.Length; j++)
                        values[j] = text.Values[j]?.ResolveObject(context, globals);
                    d.Values = values;
                }
                else if (el is BarElement bar)
                {
                    d.Type = 1;
                    d.Text = bar.Label?.LocalizedText;
                    d.Current = bar.Current?.Resolve(context, globals) ?? 0f;
                    d.Min = bar.Min?.Resolve(context, globals) ?? 0f;
                    d.Max = bar.Max?.Resolve(context, globals) ?? 100f;
                }
                else if (el is TimerElement timer)
                {
                    d.Type = 2;
                    d.Text = timer.Label?.LocalizedText;
                    d.EndTime = timer.EndTime?.Resolve(context, globals) ?? 0f;
                }
            }
            return result;
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
            if (!IsTimerRunning) return 0f;
            float remaining = _missionTimer.GetRemainingTimeInSeconds(isSynched);
            return isSynched ? MathF.Min(remaining, _missionTimer.GetTimerDuration()) : remaining;
        }

        public bool HasTimerElapsed() => IsTimerRunning && _missionTimer.Check();

        public MissionTime GetCurrentTimerStartTime() => _missionTimer.GetStartTime();
    }
}
