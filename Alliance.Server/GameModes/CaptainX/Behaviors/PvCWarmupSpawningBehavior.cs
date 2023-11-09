using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.CaptainX.Behaviors
{
    public class PvCWarmupSpawningBehavior : WarmupSpawningBehavior, ISpawnBehavior
    {
        public bool AllowExternalSpawn()
        {
            return IsRoundInProgress();
        }
    }
}
