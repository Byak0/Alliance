using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.WargAttack.NetworkMessages.FromClient;
using Alliance.Server.Core.Utils;
using Alliance.Server.Extensions.WargAttack.MissionBehavior;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.WargAttack.Handlers
{
    public class WargAttackHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestWargAttack>(HandleWargAttack);
        }

        public bool HandleWargAttack(NetworkCommunicator peer, RequestWargAttack attack)
        {
            if (peer.ControlledAgent == null)
            {
                return false;
            }

            Agent mountAgent = peer.ControlledAgent.MountAgent;

            // We determine whether to play the attack animation in motion or static
            if (mountAgent.MovementVelocity.Y >= 4)
            {
                Animation animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_attack_running");
                AnimationSystem.Instance.PlayAnimation(mountAgent, animation, true);

                WargAttack(peer, 22);
            }
            else
            {
                Animation animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_attack_stand");
                AnimationSystem.Instance.PlayAnimation(mountAgent, animation, true);

                WargAttack(peer, 36);
            }

            return true;
        }

        // Method to manage the Warg's attack
        public void WargAttack(NetworkCommunicator peer, int boneType)
        {
            if (peer.ControlledAgent == null || peer.ControlledAgent.MountAgent == null) return;

            float radius = 10f;
            float impactDistance = 0.5f;
            Agent nearbyAgent = CoreUtils.GetNearestAliveAgentInRange(radius, peer.ControlledAgent);

            if (nearbyAgent != null && nearbyAgent != peer.ControlledAgent)
            {
                float peerZAxe = peer.ControlledAgent.MountAgent.Position.z;
                float diffHauteur = Math.Abs(peerZAxe - nearbyAgent.Position.z);

                if (diffHauteur < 2f)
                {
                    Mission.Current.AddMissionBehavior(new BoneCheckDuringAnimationBehavior(
                        peer.ControlledAgent.MountAgent,
                        nearbyAgent,
                        (sbyte)boneType,
                        0.5f, // Duration of the detection. Must be lower than animation timing, the calculation of very short time intervals is not well managed by MB system
                        impactDistance,
                        (agent, target) => DealDamage(target, 60, peer.ControlledAgent)
                    ));
                }
            }
        }

        // Method called to apply damage
        public void DealDamage(Agent agent, int damageAmount, Agent damager = null)
        {
            if (agent == null || !agent.IsActive())
            {
                Log("DealDamage: tentative d'appliquer des dégâts à un agent nul ou mort.", LogLevel.Warning);
                return;
            }

            try
            {
                if (agent.State == AgentState.Active || agent.State == AgentState.Routed)
                {
                    if (agent.IsFadingOut())
                        return;

                    var damagerAgent = damager != null ? damager : agent;

                    CoreUtils.TakeDamage(agent, damagerAgent, damageAmount); // Application of the blow.
                }
            }
            catch (Exception e)
            {
                Log($"Erreur lors de l'application des dommages via DealDamage pour le Warg : {e.Message}", LogLevel.Error);
                GameNetwork.BeginModuleEventAsServer(agent.MissionPeer.GetNetworkPeer());
                GameNetwork.WriteMessage(new SendNotification("Erreur d'application des dommages pour le Warg !", 0));
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}