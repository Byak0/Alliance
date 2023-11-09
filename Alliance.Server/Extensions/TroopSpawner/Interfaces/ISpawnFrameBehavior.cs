using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.TroopSpawner.Interfaces
{
    // TODO : remove the need for this interface
    public interface ISpawnFrameBehavior
    {
        public MatrixFrame GetClosestSpawnFrame(Team team, bool hasMount, bool isInitialSpawn, MatrixFrame spawnPos);
    }
}
