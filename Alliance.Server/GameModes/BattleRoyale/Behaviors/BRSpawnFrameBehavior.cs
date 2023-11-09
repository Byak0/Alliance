using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.BattleRoyale.Behaviors
{
    public class BRSpawnFrameBehavior : SpawnFrameBehaviorBase, ISpawnFrameBehavior
    {
        public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
        {
            return GetSpawnFrameFromSpawnPoints(SpawnPoints.ToList(), null, hasMount);
        }

        public MatrixFrame GetClosestSpawnFrame(Team team, bool hasMount, bool isInitialSpawn, MatrixFrame spawnPos)
        {
            return GetSpawnFrame(team, hasMount, isInitialSpawn);
        }

        public BRSpawnFrameBehavior()
        {
        }
    }
}
