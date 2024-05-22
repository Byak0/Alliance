using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.WargAttack.NetworkMessages.FromClient;
using System;
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

            Vec3 PlayerPosition = peer.ControlledAgent.Position;
            Vec3 MountDirection = peer.ControlledAgent.MountAgent.GetMovementDirection().ToVec3();

            Agent userAgent = peer.ControlledAgent.MountAgent;

            int damageAmount = 100; // Degat appliqué à la cible
            float radius = 3f; // Radius de recherche ennemi

            MBList<Agent> nearbyAll = (MBList<Agent>)Mission.Current.AllAgents;

            //Debug
            //Common.Utilities.Logger.SendMessageToAll($"Mount direction =  {MountDirection} , playerPosition {PlayerPosition}");

            //On récupère la liste des ennemis à proximités
            MBList<Agent> nearbyAllAgents = Mission.Current.GetNearbyAgents(peer.ControlledAgent.Position.AsVec2, radius, nearbyAll);

            foreach (var agent in nearbyAllAgents)
            {
                if (agent != null && agent != peer.ControlledAgent)
                {
                    //Debug
                    //Log($"Agent {agent.Name} position {agent.Position}", LogLevel.Debug);
                    //Common.Utilities.Logger.SendMessageToAll($"Nom agent =  {agent.Name} et agent position = {agent.Position}");                    

                    //Calcul du Vecteur entre position joueur et position ennemi                    
                    Vec3 PlayerToAgent = (PlayerPosition - agent.Position).NormalizedCopy();

                    // Calculer l'angle entre la direction ddu joueur Peer et la direction de l'agent cible
                    float angleInRadians = (float)Math.Atan2(PlayerToAgent.y * MountDirection.x - PlayerToAgent.x * MountDirection.y, PlayerToAgent.x * MountDirection.x + PlayerToAgent.y * MountDirection.y);
                    float angleInDegrees = (float)(angleInRadians * (180f / Math.PI));

                    //Debug
                    //Log($"Angle =  {angleInDegrees}", LogLevel.Debug);
                    //Common.Utilities.Logger.SendMessageToAll($"Angle =  {angleInDegrees}");                    

                    //Devant = -180 ; Gauche = -90; Droite = 90; Derriere = 0
                    //Ici degat appliqué uniquement si ennemi se situe dans un angle de 45 Degree de chaque côté par rapport au centre de la direction du "Mount"
                    string position = string.Empty;
                    if (angleInDegrees < -135f && angleInDegrees > -180f)
                    {
                        position = "Front Left";
                        Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                        DealDamage(agent, damageAmount, agent.Position, peer.ControlledAgent);
                    }
                    else if (angleInDegrees < 0f && angleInDegrees > -90f)
                    {
                        position = "Back Left";
                        Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                    }
                    else if (angleInDegrees < 90f && angleInDegrees > 0f)
                    {
                        position = "Back Right";
                        Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                    }
                    else if (angleInDegrees > 135f || angleInDegrees < -180f)
                    {
                        position = "Front Right";
                        Common.Utilities.Logger.SendMessageToAll($"Position calculée =  {position}");
                        DealDamage(agent, damageAmount, agent.Position, peer.ControlledAgent);
                    }
                    else
                    {
                        position = "error ?";
                    }

                }
            }
            Animation animation = AnimationSystem.Instance.DefaultAnimations.Find(anim => anim.Name == "act_warg_attack_stand");
            AnimationSystem.Instance.PlayAnimation(userAgent, animation, true);
            return true;
        }


        // <summary>
        /// Apply damage to an agent. 
        /// </summary>
        /// <param name="agent">The agent that will be damaged</param>
        /// <param name="damageAmount">How much damage the agent will receive.</param>
        /// <param name="damager">The agent who is applying the damage</param>
        public void DealDamage(Agent agent, int damageAmount, Vec3 impactPosition, Agent damager = null)
        {

            if (agent == null || !agent.IsActive() || agent.Health < 1)
            {
                Log("DealDamage: attempted to apply damage to a null or dead agent.", LogLevel.Warning);
                return;
            }
            try
            {
                // Modifie health directement si les degats ne tuent pas.
                if (agent.State == AgentState.Active || agent.State == AgentState.Routed)
                {
                    if (agent.Health > damageAmount)
                    {
                        agent.Health -= damageAmount;

                        //Log("DoT has been applied");
                        return;
                    }

                    if (agent.IsFadingOut())
                        return;

                    var damagerAgent = damager != null ? damager : agent;

                    var blow = new Blow(damagerAgent.Index);
                    blow.DamageType = DamageTypes.Blunt;
                    blow.BoneIndex = agent.Monster.HeadLookDirectionBoneIndex;
                    blow.GlobalPosition = agent.GetChestGlobalPosition();
                    blow.BaseMagnitude = damageAmount;
                    blow.WeaponRecord.FillAsMeleeBlow(null, null, -1, -1);
                    blow.InflictedDamage = damageAmount;
                    var direction = agent.Position == impactPosition ? agent.LookDirection : agent.Position - impactPosition;
                    direction.Normalize();
                    blow.Direction = direction;
                    blow.SwingDirection = direction;
                    blow.DamageCalculated = true;
                    blow.AttackType = AgentAttackType.Kick;
                    blow.BlowFlag = BlowFlags.NoSound;
                    blow.VictimBodyPart = BoneBodyPartType.Chest;
                    blow.StrikeType = StrikeType.Thrust;


                    bool isFriendlyFire = damager != null && agent != null && damager.IsFriendOf(agent);
                    if (agent.Health <= damageAmount)
                    {
                        agent.Die(blow, Agent.KillInfo.Invalid);
                        return;
                    }
                    sbyte mainHandItemBoneIndex = damagerAgent.Monster.MainHandItemBoneIndex;

                    AttackCollisionData attackCollisionData = AttackCollisionData.GetAttackCollisionDataForDebugPurpose(
                        false,
                        false,
                        false,
                        true,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false,
                        false,
                        CombatCollisionResult.StrikeAgent,
                        -1,
                        1,
                        2,
                        blow.BoneIndex,
                        blow.VictimBodyPart,
                        mainHandItemBoneIndex,
                        Agent.UsageDirection.AttackUp,
                        -1,
                        CombatHitResultFlags.NormalHit,
                        0.5f, 1f, 0f, 0f, 0f, 0f, 0f, 0f,
                        Vec3.Up,
                        blow.Direction,
                        blow.GlobalPosition,
                        Vec3.Zero,
                        Vec3.Zero,
                        agent.Velocity,
                        Vec3.Up);
                    agent.RegisterBlow(blow, attackCollisionData);
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
