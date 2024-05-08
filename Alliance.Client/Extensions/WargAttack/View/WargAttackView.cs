using Alliance.Common.Extensions.WargAttack.NetworkMessages.FromClient;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.WargAttack.View
{
    [DefaultView]
    public class WargAttackView : MissionView
    {
        private float cooldownDuration = 1.0f; // Durée du cooldown en secondes
        private DateTime lastAttackTime = DateTime.MinValue; // Initialise le dernier moment où l'attaque a été utilisée à une valeur minimale


        public WargAttackView() { }
        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (Agent.Main != null)
            {
                //Modification à faire : Verifier touche + si joueur sur un Warg. Pour l'instant vérifie uniquement si sur une monture
                if (Agent.Main.HasMount && Input.IsKeyPressed(TaleWorlds.InputSystem.InputKey.Q))
                {
                    if (CanUseWargAttack())
                    {
                        WargAttack();
                        lastAttackTime = DateTime.UtcNow; // Met à jour le dernier moment où l'attaque a été utilisée                            
                    }
                    else
                    {
                        Log($"Warg Attack on cooldown! {TimeSpan.FromSeconds(cooldownDuration) - (DateTime.UtcNow - lastAttackTime)} secondes remaining", LogLevel.Debug);
                    }

                }
            }
        }

        private bool CanUseWargAttack()
        {
            // Vérifie si suffisamment de temps s'est écoulé depuis la dernière utilisation
            return DateTime.UtcNow - lastAttackTime >= TimeSpan.FromSeconds(cooldownDuration);
        }

        public void WargAttack()
        {


            Log($"Using WargAttack !", LogLevel.Debug);            
            
            //TODO
            //Test pour jouer une action avec la fonctionnalité attaque. Devra être refait pour partie multijoueur
            ActionIndexCache myAction = ActionIndexCache.Create("act_horse_kick");
            Agent.Main.MountAgent.SetActionChannel(0, myAction, true);

            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestWargAttack()); // Envoi message au Common
            GameNetwork.EndModuleEventAsClient();
        }
    }
}
