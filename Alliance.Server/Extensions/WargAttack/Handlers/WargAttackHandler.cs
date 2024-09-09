using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.WargAttack.NetworkMessages.FromClient;
using Alliance.Server.Core.Utils;
using System;
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

            Agent mountAgent = peer.ControlledAgent.MountAgent;

            //On détermine si on joue l'animation attaque en mouvement(avec un cooldown à l'exec du degat) ou attaque statique en se basant sur la velocité
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

        // Détection agent autour du joueur et application dégats
        public async void WargAttack(NetworkCommunicator peer, int boneType)
        {
            if (peer.ControlledAgent == null || peer.ControlledAgent.MountAgent == null) return;

            int damageAmount = 60; // Degat appliqué à la cible
            float radius = 3.5f; // Radius de recherche ennemi
            float impactDistance = 1f; //Distance acceptable d'impact/hit (Squared value si >1)

            Agent nearbyAgent = CoreUtils.GetNearestAliveAgentInRange(radius, peer.ControlledAgent);

            if (nearbyAgent != null && nearbyAgent != peer.ControlledAgent)
            {
                //Debug
                //Log($"Agent {agent.Name} position {agent.Position}", LogLevel.Debug);
                //Common.Utilities.Logger.SendMessageToAll($"Nom agent =  {nearbyAgent.Name} et agent Distance {PlayerPosition.Distance(nearbyAgent.Position)}");                    

                //Calcul hauteur pour ne pas tuer un agent trop haut ou trop bas
                float peerZAxe = peer.ControlledAgent.MountAgent.Position.z;
                float diffHauteur = Math.Abs(peerZAxe - nearbyAgent.Position.z);

                //boneType = 36 en dure pour le moment, evol à faire pour ajouter une liste
                bool check = await CheckBoneDuringAnimation(peer.ControlledAgent.MountAgent, nearbyAgent, 36, 1, impactDistance);
                if (check && diffHauteur < 2f)
                {
                    System.Diagnostics.Debug.WriteLine($"DANS MAIN APPLIQUER DES DEGATS");
                    DealDamage(nearbyAgent, damageAmount, peer.ControlledAgent);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"PAS de degat applique dans MAIN");
                }

            }
        }

        /// <summary>
        /// Find the first bone in selected range
        /// </summary>
        /// <param name="agent">Player mount agent</param>
        /// <param name="target">Target</param>
        /// <param name="boneType">ID of the agent bone to compare distance with target</param>
        /// <param name="rangeSquarded">Range square of maximum allowed range</param>
        /// <returns>Bone id if in range else -1</returns>
        public static sbyte SearchBoneInRange(Agent agent, Agent target, sbyte boneType, float rangeSquarded)
        {
            System.Diagnostics.Debug.WriteLine($"J'entre dans searchBoneInRange");
            MatrixFrame agentGlobalFrame = agent.AgentVisuals.GetGlobalFrame(); // recupère frame dans scene globale
            MatrixFrame cibleGlobalFrame = target.AgentVisuals.GetGlobalFrame(); // recupère frame dans scene globale          
            MatrixFrame agentBone = agent.AgentVisuals.GetSkeleton().GetBoneEntitialFrameWithIndex(boneType); // Récupère info du bone patte warg Fingers2_R (36)                                                                                                             

            Vec3 agentBoneGlobalPosition = agentGlobalFrame.TransformToParent(agentBone.origin);//Transpose le bone agent(scene agent) dans la scene mission

            sbyte closestBone = -1;
            float closestDistanceSquared = float.MaxValue;

            int targetBoneCount = target.AgentVisuals.GetSkeleton().GetBoneCount();

            System.Diagnostics.Debug.WriteLine($"Début de la recherche de bone le plus proche. Nombre de bones dans la cible: {targetBoneCount}");
            System.Diagnostics.Debug.WriteLine($"debut du FOR");
            for (int i = 0; i < targetBoneCount; i++)
            {
                //Get bone frame, then set it to scene coordinate
                MatrixFrame targetBoneFrame = target.AgentVisuals.GetSkeleton().GetBoneEntitialFrameWithIndex((sbyte)i);
                Vec3 targetBoneGlobalPosition = cibleGlobalFrame.TransformToParent(targetBoneFrame.origin);

                float distanceSquared = (targetBoneGlobalPosition - agentBoneGlobalPosition).LengthSquared;

                System.Diagnostics.Debug.WriteLine($"Bone ID: {i}, Position: {targetBoneGlobalPosition}, Distance au carré: {distanceSquared}");

                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestBone = (sbyte)i;//Keep it just in case for future use...               

                    System.Diagnostics.Debug.WriteLine($"Nouveau bone le plus proche: {closestBone}, Distance au carré: {closestDistanceSquared}");
                }

                System.Diagnostics.Debug.WriteLine($"DistancesSquared: {distanceSquared}, rangeSquarded : {rangeSquarded}");
                if (distanceSquared <= rangeSquarded)
                {
                    System.Diagnostics.Debug.WriteLine($"Bone ID: {i} est dans la plage de distance requise. Retourne {closestBone}");
                    return closestBone;
                }
            }

            // DEBUG : Affiche les résultats
            if (closestBone == -1)
            {
                Common.Utilities.Logger.SendMessageToAll("Aucun bone trouvé .");
                System.Diagnostics.Debug.WriteLine("Aucun bone trouvé dans la cible.");
            }
            System.Diagnostics.Debug.WriteLine("Arrive a la fin de search sans resultat");
            return closestBone = -1;
        }

        /// <summary>
        /// Find a bone to hit during animation
        /// </summary>
        /// <param name="agent">Player Mount</param>
        /// <param name="target">Closest target</param>
        /// <param name="agentBoneType">ID of the bone needed to hit the target</param>
        /// <param name="duration">duration of animation</param>
        /// <param name="minDistanceSquared">minimal distance to hit the target</param>
        /// <returns>True if target bone found in range else false</returns>
        public static async Task<bool> CheckBoneDuringAnimation(Agent agent, Agent target, sbyte agentBoneType, int duration, float minDistanceSquared)
        {

            float startTime = Mission.Current.CurrentTime;
            //float endTime = startTime + duration;
            try
            {

                while (Mission.Current.CurrentTime - startTime < duration)
                {
                    System.Diagnostics.Debug.WriteLine($"CurrentTime = {Mission.Current.CurrentTime - startTime} et EndTime = {duration}");
                    if (agent == null || target == null || !agent.IsActive() || !target.IsActive())
                    {
                        Common.Utilities.Logger.SendMessageToAll("Agent ou cible invalide.");
                        System.Diagnostics.Debug.WriteLine("Agent or target is null or inactive.");
                        return false;
                    }

                    sbyte closestBone = SearchBoneInRange(agent, target, agentBoneType, minDistanceSquared);

                    // Vérifier si un bone a été trouvé
                    if (closestBone == -1)
                    {
                        System.Diagnostics.Debug.WriteLine("Aucun bone trouvé à la bonne distance lors de la recherche.");
                        await Task.Delay(400); // Attendre avant de réessayer
                        continue;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.Utilities.Logger.SendMessageToAll($"Exception: {ex.Message}");
                return false;
            }

            // Si la distance minimale n'a pas été atteinte pendant la durée de l'animation
            Common.Utilities.Logger.SendMessageToAll("Animation terminée, distance minimale non atteinte.");
            return false;
        }

        /// <summary>
        /// Apply damage to an agent
        /// </summary>
        /// <param name="target">The agent that will be hit</param>
        /// <param name="damageAmount">How much damage the agent will receive</param>
        /// <param name="damager">The agent who is applying the damage</param>
        public void DealDamage(Agent target, int damageAmount, Agent damager = null)
        {

            if (target == null || !target.IsActive())
            {
                Log("DealDamage: attempted to apply damage to a null or dead agent.", LogLevel.Warning);
                return;
            }
            try
            {
                if (target.State == AgentState.Active || target.State == AgentState.Routed)
                {
                    if (target.IsFadingOut())
                        return;

                    var damagerAgent = damager != null ? damager : target;

                    CoreUtils.TakeDamage(target, damagerAgent, damageAmount); // Application du blow
                }
            }
            catch (Exception e)
            {
                Log($"Impossible d'appliquer des dommages via DealDamage pour le Warg. Voir {e.Message}", LogLevel.Error);
                GameNetwork.BeginModuleEventAsServer(target.MissionPeer.GetNetworkPeer());
                GameNetwork.WriteMessage(new SendNotification("Une erreur d'application de degat pour le Warg est survenue !", 0));
                GameNetwork.EndModuleEventAsServer();
            }
        }
    }
}
