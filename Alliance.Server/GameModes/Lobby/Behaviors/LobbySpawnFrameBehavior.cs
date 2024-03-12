using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Lobby.Behaviors
{
    public class LobbySpawnFrameBehavior : SpawnFrameBehaviorBase, ISpawnFrameBehavior
    {
        public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
        {
            return GetSpawnFrameFromSpawnPoints(SpawnPoints.ToList(), null, hasMount);
        }

        public MatrixFrame GetClosestSpawnFrame(Team team, bool hasMount, bool isInitialSpawn, MatrixFrame spawnPos)
        {
            return GetBestClosestSpawnFrame(SpawnPoints.ToList(), hasMount, spawnPos);
        }

        private MatrixFrame GetBestClosestSpawnFrame(List<GameEntity> spawnPointList, bool hasMount, MatrixFrame spawnPos)
        {
            float bestSpawnScore = float.MinValue;
            MatrixFrame bestSpawn = new MatrixFrame();
            int nbAgentsAtBest = 0;
            MBList<Agent> agents = (MBList<Agent>)Mission.Current.Agents;
            foreach (GameEntity entity in SpawnPoints)
            {
                float spawnScore = 0;
                // The farther away the spawnPoint is from spawnPos, the lesser it scores
                spawnScore -= Math.Abs((spawnPos.origin - entity.GlobalPosition).Length);
                // The more agents are already close to the spawn, the less it scores
                int nbAgents = Mission.Current.GetNearbyAgents(entity.GlobalPosition.AsVec2, 5f, agents).Count();
                spawnScore -= nbAgents;

                if (hasMount && entity.HasTag("exclude_mounted"))
                {
                    spawnScore -= 100f;
                }
                if (spawnScore > bestSpawnScore)
                {
                    nbAgentsAtBest = nbAgents;
                    bestSpawnScore = spawnScore;
                    bestSpawn = entity.GetGlobalFrame();
                }
            }
            if (nbAgentsAtBest > 30)
            {
                Log($"Too much agents already spawned at this position, spawning slightly to the side.", LogLevel.Warning);
                bestSpawn.Advance(MBRandom.RandomFloatRanged(-10f, 10f));
                bestSpawn.Strafe(MBRandom.RandomFloatRanged(-10f, 10f));
            }
            bestSpawn.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            return bestSpawn;
        }

        public LobbySpawnFrameBehavior()
        {
        }
    }
}
