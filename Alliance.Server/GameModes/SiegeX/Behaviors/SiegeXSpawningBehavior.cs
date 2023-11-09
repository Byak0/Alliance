using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.SiegeX.Behaviors
{
    public class SiegeXSpawningBehavior : SiegeSpawningBehavior, ISpawnBehavior
    {
        public bool AllowExternalSpawn()
        {
            return IsRoundInProgress();
        }
    }
}