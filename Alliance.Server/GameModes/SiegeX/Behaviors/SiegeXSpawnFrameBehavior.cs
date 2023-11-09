using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.SiegeX.Behaviors
{
    public class SiegeXSpawnFrameBehavior : SiegeSpawnFrameBehavior, ISpawnFrameBehavior
    {
        public MatrixFrame GetClosestSpawnFrame(Team team, bool hasMount, bool isInitialSpawn, MatrixFrame spawnPos)
        {
            return GetSpawnFrame(team, hasMount, isInitialSpawn);
        }
    }
}