using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.TroopSpawner.Handlers
{
    public class SpawnTroopHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SpawnInfoMessage>(HandleSpawnInfoMessage);
            reg.Register<FormationControlMessage>(HandleFormationControlMessage);
            reg.Register<BotsControlledChange>(HandleServerEventBotsControlledChangeEvent);
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

        public void HandleServerEventBotsControlledChangeEvent(BotsControlledChange message)
        {
            MissionPeer component = message.Peer.GetComponent<MissionPeer>();
            MissionMultiplayerGameModeBaseClient gameModeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            if (gameModeClient is IBotControllerBehavior) ((IBotControllerBehavior)gameModeClient)?.OnBotsControlledChanged(component, message.AliveCount, message.TotalCount);
        }
    }
}
