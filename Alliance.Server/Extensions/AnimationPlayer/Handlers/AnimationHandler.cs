using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.AnimationPlayer.NetworkMessages.FromClient;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.AnimationPlayer.Handlers
{
    public class AnimationHandler : IHandlerRegister
    {
        public AnimationHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestAnimation>(HandleAnimationRequest);
            reg.Register<RequestAnimationFormation>(HandleAnimationFormationRequest);
        }

        public bool HandleAnimationRequest(NetworkCommunicator peer, RequestAnimation message)
        {
            try
            {
                // Check if peer is authorized to request animation
                if ((peer.ControlledAgent == null || peer.ControlledAgent != message.UserAgent) && !peer.IsAdmin()) return false;
                if (!IsAnimationAuthorized(message.ActionIndex) && !peer.IsAdmin()) return false;

                Log($"Alliance - {peer.UserName} is requesting to play animation {message.ActionIndex} for player {message.UserAgent.Name}", LogLevel.Information);
                AnimationSystem.Instance.PlayAnimation(message.UserAgent, new Animation(message.ActionIndex, speed: message.Speed), true);

                return true;
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play animation {message.ActionIndex} for player {message.UserAgent.Name} (requested by {peer.UserName})", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
            return false;
        }

        public bool HandleAnimationFormationRequest(NetworkCommunicator peer, RequestAnimationFormation message)
        {
            try
            {
                Formation formation = message.Team.GetFormation((FormationClass)message.FormationIndex);

                // Check if peer is authorized to request animation
                if (peer.ControlledAgent != formation.PlayerOwner && !peer.IsAdmin()) return false;
                if (!IsAnimationAuthorized(message.ActionIndex) && !peer.IsAdmin()) return false;

                Log($"Alliance - {peer.UserName} is requesting to play animation {message.ActionIndex} for formation {formation.Index}", LogLevel.Information);
                AnimationSystem.Instance.PlayAnimationForFormation(formation, new Animation(message.ActionIndex, speed: message.Speed), true);

                return true;
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play animation {message.ActionIndex} for formation {message.FormationIndex} (requested by {peer.UserName})", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
            return false;
        }

        /// <summary>
        /// Check if requested action is part of the default animations authorized
        /// </summary>
        public bool IsAnimationAuthorized(int actionIndex)
        {
            foreach (AnimationSequence animSeq in AnimationUserStore.Instance.AnimationSequences)
            {
                foreach (Animation anim in animSeq.Animations)
                {
                    if (anim.Index == actionIndex) return true;
                }
            }
            return false;
        }
    }
}
