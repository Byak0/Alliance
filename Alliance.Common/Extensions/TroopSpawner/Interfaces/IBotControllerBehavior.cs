using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.TroopSpawner.Interfaces
{
    public interface IBotControllerBehavior
    {
        void OnBotsControlledChanged(MissionPeer component, int aliveCount, int totalCount);
    }
}