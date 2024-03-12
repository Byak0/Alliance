using Alliance.Common.Extensions.TroopSpawner.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.TroopSpawner.Behaviors
{
    /// <summary>
    /// Behavior managing bot recruitment and player control over formations.
    /// Automatically gives back control when a commander respawn or take control of an ally bot.
    /// </summary>
    public class TroopSpawnerBehavior : MissionNetwork, IMissionBehavior
    {
        private float _tick;
        private MultiplayerRoundController _roundController;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _roundController = Mission.Current.GetMissionBehavior<MultiplayerRoundController>();
        }

        public override void AfterStart()
        {
            base.AfterStart();
            if (_roundController != null)
            {
                _roundController.OnRoundEnding += ClearAgentAfterRound;
            }
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            if (_roundController != null)
            {
                _roundController.OnRoundEnding -= ClearAgentAfterRound;
            }
        }

        private void ClearAgentAfterRound()
        {
            Log($"Round end - Clearing all agents infos...", LogLevel.Debug);
            AgentsInfoModel.Instance.ClearAllAgentInfos();
        }

        public override void OnMissionTick(float dt)
        {
            _tick += dt;
            if (_tick > 1f)
            {
                _tick = 0f;
                AgentsInfoModel.Instance.CheckAndRemoveExpiredAgents();
            }
        }

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
            AgentsInfoModel.Instance.MarkAgentInfoAsExpiredWithDelay(affectedAgent);
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