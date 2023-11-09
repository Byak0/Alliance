using Alliance.Common.GameModes.Story.Models;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using Alliance.Server.GameModes.Story.Behaviors.SpawningStrategy;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Behaviors
{
    /// <summary>
    /// Configurable spawning class for Scenario.
    /// Delegate most logic to the current SpawnStragegy.
    /// </summary>
    public class ScenarioSpawningBehavior : SpawningBehaviorBase, ISpawnBehavior
    {
        public ISpawningStrategy SpawningStrategy { get; private set; }

        private List<KeyValuePair<MissionPeer, Timer>> _enforcedSpawnTimers;

        public ScenarioSpawningBehavior()
        {
            _enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
        }

        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);

            ScenarioManagerServer.Instance.OnActStateSpawnParticipants += StartSpawnSession;
            ScenarioManagerServer.Instance.OnActStateDisplayResults += EndSpawnSession;

            SpawningStrategy = new SpawningStrategyBase();
            SpawningStrategy.Initialize(SpawnComponent, this, SpawnComponent.SpawnFrameBehavior);
        }

        public void SetSpawningStrategy(ISpawningStrategy spawningStrategy)
        {
            SpawningStrategy = spawningStrategy;
            SpawningStrategy.Initialize(SpawnComponent, this, SpawnComponent.SpawnFrameBehavior);
        }

        public override void Clear()
        {
            base.Clear();

            ScenarioManagerServer.Instance.OnActStateSpawnParticipants -= StartSpawnSession;
            ScenarioManagerServer.Instance.OnActStateDisplayResults -= EndSpawnSession;
        }

        public override void OnTick(float dt)
        {
            SpawningStrategy.OnTick(dt);
        }

        protected override void SpawnAgents()
        {
        }

        public void StartSpawnSession()
        {
            IsSpawningEnabled = true;
            SpawningStrategy.StartSpawnSession();
        }

        public void EndSpawnSession()
        {
            IsSpawningEnabled = false;
            SpawningStrategy.EndSpawnSession();
        }

        public void PauseSpawn()
        {
            IsSpawningEnabled = false;
            SpawningStrategy.PauseSpawnSession();
        }

        public void ResumeSpawn()
        {
            IsSpawningEnabled = true;
            SpawningStrategy.ResumeSpawnSession();
        }

        public override void OnClearScene()
        {
            SpawningStrategy.OnClearScene();
        }

        bool ISpawnBehavior.AllowExternalSpawn()
        {
            return ScenarioManagerServer.Instance.ActState == ActState.InProgress;
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
        {
            return true;
        }

        protected override bool IsRoundInProgress()
        {
            return ScenarioManagerServer.Instance.ActState == ActState.InProgress;
        }

        /// <summary>
        /// Creates an enforced spawn timer (EST) for a specific peer, forcing the peer to wait a certain duration before spawning.
        /// </summary>
        /// <param name="peer">The peer for whom the timer is being set.</param>
        /// <param name="durationInSeconds">The duration of the timer in seconds.</param>
        public void CreateEnforcedSpawnTimerForPeer(MissionPeer peer, int durationInSeconds)
        {
            try
            {
                // Check if peer is invalid or already has an EST
                if (peer == null || _enforcedSpawnTimers.Exists(pair => pair.Key == peer))
                {
                    return;
                }

                // Create a new timer with a set duration, and add it to the list of timers
                // The timer starts at the current mission time and ends after the specified duration
                _enforcedSpawnTimers.Add(new KeyValuePair<MissionPeer, Timer>(peer, new Timer(Mission.CurrentTime, durationInSeconds, true)));

                Log($"EST for {peer.Name} set to {durationInSeconds} seconds.", LogLevel.Debug);
            }
            catch (Exception e)
            {
                Log("DEBUG : ERROR in CreateEnforcedSpawnTimerForPeer", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
            }
        }

        /// <summary>
        /// Checks if the enforced spawn timer (EST) for a given peer has expired.
        /// </summary>
        /// <param name="peer">The peer whose timer is being checked.</param>
        /// <param name="checkForVisuals">Whether to check if the peer has agent visuals (optional; defaults to true).</param>
        /// <returns>True if the timer has expired, false otherwise.</returns>
        public bool CheckIfEnforcedSpawnTimerExpiredForPeer(MissionPeer peer, bool checkForVisuals = true)
        {
            // Get the entry in the timer list for the given peer
            KeyValuePair<MissionPeer, Timer> peerTimerEntry = _enforcedSpawnTimers.FirstOrDefault(entry => entry.Key == peer);

            // If there is no entry for the given peer in the list, the timer has not expired
            if (peerTimerEntry.Key == null)
            {
                return false;
            }

            // If the peer already has a controlled agent, they have already spawned,
            // so the timer is no longer valid and the entry is removed from the list
            if (peer.ControlledAgent != null)
            {
                _enforcedSpawnTimers.RemoveAll(entry => entry.Key == peer);
                Log($"EST for {peer.Name} is no longer valid (spawned already).", LogLevel.Debug);
                return false;
            }

            // Retrieve the timer from the key-value pair
            Timer timer = peerTimerEntry.Value;

            // If checkForVisuals is true, the peer has agent visuals and the timer has elapsed (i.e., the current time is greater than the timer's end time),
            // then the enforced spawn timer has expired
            // If checkForVisuals is false, only check if the timer has elapsed
            if ((!checkForVisuals || peer.HasSpawnedAgentVisuals) && timer.Check(Mission.Current.CurrentTime))
            {
                // Despawn the agent visuals and remove the entry from the list, as the timer has expired
                SpawnComponent.SetEarlyAgentVisualsDespawning(peer, true);
                _enforcedSpawnTimers.RemoveAll(entry => entry.Key == peer);
                Log($"EST for {peer.Name} has expired.", LogLevel.Debug);

                // Return true to indicate that the timer has expired
                return true;
            }

            // If none of the above conditions are met, the timer has not expired
            return false;
        }
    }
}
