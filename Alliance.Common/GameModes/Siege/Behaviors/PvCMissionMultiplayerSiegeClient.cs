using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Siege.Behaviors
{
    public class PvCMissionMultiplayerSiegeClient : MissionMultiplayerSiegeClient, IBotControllerBehavior
    {
        public void OnBotsControlledChanged(MissionPeer component, int aliveCount, int totalCount) { }
    }
}