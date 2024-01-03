using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Common.GameModels;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.FormationEnforcer.Behavior
{
    /// <summary>
    /// Check formation state of players.
    /// </summary>
    public class FormationBehavior : MissionNetwork
    {
        private float _lastFormationCheck;

        public FormationBehavior() : base()
        {
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            networkPeer.AddComponent<FormationComponent>();
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // Add buff / debuff depending on formation
            // Check every sec max
            _lastFormationCheck += dt;
            if (_lastFormationCheck >= 0.5f)
            {
                _lastFormationCheck = 0;
                foreach (NetworkCommunicator player in GameNetwork.NetworkPeers)
                {
                    CheckPlayerInFormation(player);
                }
            }
        }

        private void CheckPlayerInFormation(NetworkCommunicator player)
        {
            FormationComponent playerRep = player.GetComponent<FormationComponent>();

            if (playerRep == null || player.ControlledAgent?.Team == null)
            {
                return;
            }

            if (player.ControlledAgent.Team.ActiveAgents.Count <= Config.Instance.MinPlayerForm)
            {
                playerRep.State = FormationState.Formation;
                return;
            }

            bool ownFormationOnly = !player.IsCommander();

            if (FormationCalculateModel.IsInFormation(player.ControlledAgent, ownFormationOnly))
            {
                playerRep.State = FormationState.Formation;
            }
            else if (FormationCalculateModel.IsInSkirmish(player.ControlledAgent, ownFormationOnly))
            {
                playerRep.State = FormationState.Skirmish;
            }
            else
            {
                playerRep.State = FormationState.Rambo;
            }
        }

        // Reset everyone's state on mission end
        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();

            foreach (NetworkCommunicator player in GameNetwork.NetworkPeers)
            {
                FormationComponent playerRep = player.GetComponent<FormationComponent>();

                if (playerRep != null)
                {
                    playerRep.State = FormationState.Formation;
                }
            }
        }
    }
}