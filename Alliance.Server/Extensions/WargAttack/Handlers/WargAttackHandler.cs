using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.WargAttack.NetworkMessages.FromClient;
using Alliance.Server.Core.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
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

            Agent userAgent = peer.ControlledAgent.MountAgent;
            int waitTime = 800;//1000 = 1s. Temps d'attente entre début animation en mouvement et le dégat

            //On détermine si on joue l'animation attaque en mouvement(avec un cooldown l'exec du degat) ou attaque statique en se basant sur la velocité
            if (peer.ControlledAgent.MountAgent.MovementVelocity.Y >= 4)
            {
                Animation animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_attack_running");
                AnimationSystem.Instance.PlayAnimation(userAgent, animation, true);

                wargAttack(peer, waitTime);
            }
            else
            {
                Animation animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_attack_stand");
                AnimationSystem.Instance.PlayAnimation(userAgent, animation, true);

                wargAttack(peer, 0);
            }

            return true;
        }

        // Détection agent autour du joueur et application dégats
        public async void wargAttack(NetworkCommunicator peer, int waitTime)
        {
            await Task.Delay(waitTime);

            if (peer.ControlledAgent == null || peer.ControlledAgent.MountAgent == null) return;

            Vec3 PlayerPosition = peer.ControlledAgent.Position;
            Vec3 MountDirection = peer.ControlledAgent.MountAgent.GetMovementDirection().ToVec3();

            int damageAmount = 60; // Degat appliqué à la cible
            float radius = 3.5f; // Radius de recherche ennemi

            //Debug
            //Common.Utilities.Logger.SendMessageToAll($"Mount direction =  {MountDirection} , playerPosition {PlayerPosition}");

            List<Agent> nearbyAllAgents = CoreUtils.GetNearAliveAgentsInRange(radius, peer.ControlledAgent);

            foreach (var agent in nearbyAllAgents)
            {
                if (agent != null && agent != peer.ControlledAgent)
                {
                    //Debug
                    //Log($"Agent {agent.Name} position {agent.Position}", LogLevel.Debug);
                    //Common.Utilities.Logger.SendMessageToAll($"Nom agent =  {agent.Name} et agent position = {agent.Position}");                    

                    //Calcul du Vecteur entre position joueur et position ennemi                    
                    Vec3 PlayerToAgent = (PlayerPosition - agent.Position).NormalizedCopy();

                    // Calculer l'angle entre la direction du joueur Peer et la direction de l'agent cible
                    float angleInRadians = (float)Math.Atan2(PlayerToAgent.y * MountDirection.x - PlayerToAgent.x * MountDirection.y, PlayerToAgent.x * MountDirection.x + PlayerToAgent.y * MountDirection.y);
                    float angleInDegrees = (float)(angleInRadians * (180f / Math.PI));

                    //Calcul hauteur pour ne pas tuer un agent trop haut ou trop bas
                    float peerZAxe = peer.ControlledAgent.MountAgent.Position.z;
                    float diffHauteur = Math.Abs(peerZAxe - agent.Position.z);

                    //Debug
                    //Log($"Angle =  {angleInDegrees}", LogLevel.Debug);
                    //Common.Utilities.Logger.SendMessageToAll($"Angle =  {angleInDegrees}");                    

                    //Devant = -180 ; Gauche = -90; Droite = 90; Derriere = 0
                    //Ici degat appliqué uniquement si ennemi se situe dans un angle de 45 Degree de chaque côté par rapport au centre de la direction du "Mount"
                    string position = string.Empty;
                    if (angleInDegrees < -160f && angleInDegrees > -180f && diffHauteur < 2f)
                    {
                        position = "Front Left";
                        // Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                        DealDamage(agent, damageAmount, agent.Position, peer.ControlledAgent);
                    }
                    else if (angleInDegrees < 0f && angleInDegrees > -90f)
                    {
                        position = "Back Left";
                        //Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                    }
                    else if (angleInDegrees < 90f && angleInDegrees > 0f)
                    {
                        position = "Back Right";
                        //Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                    }
                    else if ((angleInDegrees > 160f || angleInDegrees < -180f) && diffHauteur < 2f)
                    {
                        position = "Front Right";
                        //Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                        DealDamage(agent, damageAmount, agent.Position, peer.ControlledAgent);
                    }
                    else
                    {
                        position = "error ?";
                    }

                }
            }
        }

        // <summary>
        /// Apply damage to an agent. 
        /// </summary>
        /// <param name="agent">The agent that will be damaged</param>
        /// <param name="damageAmount">How much damage the agent will receive.</param>
        /// <param name="damager">The agent who is applying the damage</param>
        public void DealDamage(Agent agent, int damageAmount, Vec3 impactPosition, Agent damager = null)
        {

            if (agent == null || !agent.IsActive())
            {
                Log("DealDamage: attempted to apply damage to a null or dead agent.", LogLevel.Warning);
                return;
            }
            try
            {
                // Modifie health directement si les degats ne tuent pas.
                if (agent.State == AgentState.Active || agent.State == AgentState.Routed)
                {
                    if (agent.IsFadingOut())
                        return;

                    var damagerAgent = damager != null ? damager : agent;

                    CoreUtils.TakeDamage(agent, damagerAgent, damageAmount); // Application du blow
                }
            }
            catch (Exception e)
            {
                Log($"Impossible d'appliquer des dommages via DealDamage pour le Warg. Voir {e.Message}", LogLevel.Error);
                GameNetwork.BeginModuleEventAsServer(agent.MissionPeer.GetNetworkPeer());
                GameNetwork.WriteMessage(new SendNotification("Une erreur d'application de degat pour le Warg est survenue !", 0));
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}
