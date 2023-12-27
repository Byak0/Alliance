using Alliance.Common.Extensions.TroopSpawner.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.TroopSpawner.Behaviors
{
    /// <summary>
    /// Behavior managing player control over formations.
    /// Automatically gives back control when a commander respawn or take control of an ally bot.
    /// </summary>
    public class FormationControlBehavior : MissionNetwork, IMissionBehavior
    {
        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            FormationControlModel.Instance.SendMappingToClient(networkPeer);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            FormationControlModel.Instance.ReassignControlToAgent(agent);
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