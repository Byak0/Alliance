using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.TroopSpawner.Behaviors
{
    /// <summary>
    /// Behavior managing bot recruitment and player control over formations.
    /// Automatically gives back control when a commander respawn or take control of an ally bot.
    /// </summary>
    public class TroopSpawnerBehavior : MissionNetwork, IMissionBehavior
    {
        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            FormationControlModel.Instance.SendMappingToClient(networkPeer);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            FormationControlModel.Instance.ReassignControlToAgent(agent);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            SpawnHelper.RemoveBot(affectedAgent);
        }

        protected override void OnAgentControllerChanged(Agent agent, Agent.ControllerType oldController)
        {
            if (oldController != Agent.ControllerType.Player && agent.IsPlayerControlled)
            {
                FormationControlModel.Instance.ReassignControlToAgent(agent);
            }
        }
    }
}