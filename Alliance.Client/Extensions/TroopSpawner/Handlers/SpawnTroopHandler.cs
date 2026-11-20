using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.TroopSpawner.Handlers
{
	public class SpawnTroopHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SpawnInfoMessage>(HandleSpawnInfoMessage);
			reg.Register<FormationControlMessage>(HandleFormationControlMessage);
			reg.Register<BotsControlledChange>(HandleBotsControlledChange);
			reg.Register<SetHealthLimitMessage>(HandleSetHealthLimit);
		}

		public void HandleSpawnInfoMessage(SpawnInfoMessage message)
		{
			SpawnTroopsModel.Instance.RefreshTroopSpawn(message.Troop, message.TroopCount);
			SpawnTroopsModel.Instance.RefreshFormations();
		}

		public void HandleFormationControlMessage(FormationControlMessage message)
		{
			MissionPeer target = message.Peer.GetComponent<MissionPeer>();
			if (message.Delete)
			{
				FormationControlModel.Instance.RemoveControlFromPlayer(target, message.TeamIndex, message.Formation);
			}
			else
			{
				FormationControlModel.Instance.AssignControlToPlayer(target, message.TeamIndex, message.Formation);
			}
		}

		public void HandleBotsControlledChange(BotsControlledChange message)
		{
			MissionPeer component = message.Peer.GetComponent<MissionPeer>();
			MissionMultiplayerGameModeBaseClient gameModeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
			if (gameModeClient is IBotControllerBehavior) ((IBotControllerBehavior)gameModeClient)?.OnBotsControlledChanged(component, message.AliveCount, message.TotalCount);
		}

		public void HandleSetHealthLimit(SetHealthLimitMessage message)
		{
			Agent agent = Mission.MissionNetworkHelper.GetAgentFromIndex(message.AgentIndex);
			if (agent != null)
			{
				agent.HealthLimit = message.HealthLimit;
				Log($"Synced agent {agent.Index} health limit: {message.HealthLimit}", LogLevel.Debug);
			}
		}
	}
}
