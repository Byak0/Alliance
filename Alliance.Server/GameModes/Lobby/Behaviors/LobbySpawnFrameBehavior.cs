using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.GameModes.Lobby.Behaviors
{
    public class LobbySpawnFrameBehavior : SpawnFrameBehaviorBase, ISpawnFrameBehavior
    {
        public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
        {
            return GetBestSpawnPoint(SpawnPoints.ToList(), hasMount);
        }

        private MatrixFrame GetBestSpawnPoint(List<GameEntity> spawnPointList, bool hasMount)
        {
            float bestSpawnScore = float.MinValue;
            int bestSpawnIndex = -1;
            MBList<Agent> agents = (MBList<Agent>)Mission.Current.Agents;
            for (int i = 0; i < spawnPointList.Count; i++)
            {
                float spawnScore = MBRandom.RandomFloat * 0.2f;
                foreach (Agent agent in Mission.Current.GetNearbyAgents(spawnPointList[i].GlobalPosition.AsVec2, 2f, agents))
                {
                    float lengthSquared = (agent.Position - spawnPointList[i].GlobalPosition).LengthSquared;
                    if (lengthSquared < 4f)
                    {
                        float length = MathF.Sqrt(lengthSquared);
                        spawnScore -= (2f - length) * 5f;
                    }
                }
                if (hasMount && spawnPointList[i].HasTag("exclude_mounted"))
                {
                    spawnScore -= 100f;
                }
                if (spawnScore > bestSpawnScore)
                {
                    bestSpawnScore = spawnScore;
                    bestSpawnIndex = i;
                }
            }
            MatrixFrame globalFrame = spawnPointList[bestSpawnIndex].GetGlobalFrame();
            globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            return globalFrame;
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
