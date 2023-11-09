using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.ExtendedCharacter.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.TroopSpawner.Handlers
{
    public class SpawnTroopHandler
    {
        public SpawnTroopHandler()
        {
        }

        public void HandleSpawnInfoMessage(SpawnInfoMessage message)
        {
            message.Troop.GetExtendedCharacterObject().TroopLeft = message.TroopLeft;
            SpawnTroopsModel.Instance.RefreshTroopSpawn(message.Troop, message.TroopCount);
            SpawnTroopsModel.Instance.RefreshFormations();
        }

        public void HandleFormationControlMessage(FormationControlMessage message)
        {
            MissionPeer target = message.Peer.GetComponent<MissionPeer>();
            if (message.Delete)
            {
                FormationControlModel.Instance.RemoveControlFromPlayer(target, message.Formation);
            }
            else
            {
                FormationControlModel.Instance.AssignControlToPlayer(target, message.Formation);
            }
        }
    }
}
