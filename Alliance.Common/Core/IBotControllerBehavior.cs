using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Core
{
    public interface IBotControllerBehavior
    {
        void OnBotsControlledChanged(MissionPeer component, int aliveCount, int totalCount);
    }
}