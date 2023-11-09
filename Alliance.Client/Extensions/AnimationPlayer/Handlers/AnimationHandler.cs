using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.AnimationPlayer.NetworkMessages.FromServer;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Client.Extensions.AnimationPlayer.Handlers
{
    public class AnimationHandler : IHandlerRegister
    {
        public AnimationHandler()
        {
        }

        public void Register(NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncAnimation>(SyncAnimation);
            reg.Register<SyncAnimationFormation>(SyncAnimationFormation);
        }

        public void SyncAnimation(SyncAnimation message)
        {
            try
            {
                AnimationSystem.Instance.PlayAnimation(message.UserAgent, new Animation(message.Index, speed: message.Speed));
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play animation {message.Index} for player {message.UserAgent.Name}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        public void SyncAnimationFormation(SyncAnimationFormation message)
        {
            try
            {
                Formation formation = message.Team.GetFormation((FormationClass)message.FormationIndex);
                AnimationSystem.Instance.PlayAnimationForFormation(formation, new Animation(message.ActionIndex, speed: message.Speed));
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play animation {message.ActionIndex} for formation {message.FormationIndex}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
