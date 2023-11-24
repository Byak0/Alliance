using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.AnimationPlayer.NetworkMessages.FromClient;
using System;
using System.Threading.Tasks;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.AnimationPlayer
{
    /// <summary>
    /// Sends request to the server to play animations.
    /// </summary>
    public class AnimationRequestEmitter
    {
        public float LastRequest { get; set; }

        private static AnimationRequestEmitter _instance;
        public static AnimationRequestEmitter Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AnimationRequestEmitter();
                }
                return _instance;
            }
        }

        public AnimationRequestEmitter() { }

        public async void RequestAnimationSequenceForTarget(AnimationSequence animationSequence, Agent target)
        {
            if (target == null || animationSequence == null || animationSequence.Animations == null) return;
            if (LastRequest + 1 > Mission.Current.CurrentTime) return;
            LastRequest = Mission.Current.CurrentTime;
            try
            {
                foreach (Animation animation in animationSequence.Animations)
                {
                    Log($"Playing {animation.Name}", LogLevel.Debug);
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new RequestAnimation(target, animation.Index, animation.Speed));
                    GameNetwork.EndModuleEventAsClient();
                    await Task.Delay(TimeSpan.FromSeconds(animation.MaxDuration));
                }
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failure in sequence {animationSequence.Name} on agent {target.Name}", LogLevel.Error);
                Log(ex.Message, LogLevel.Error);
            }
        }

        public async void RequestAnimationSequenceForFormation(AnimationSequence animationSequence, Formation target)
        {
            if (target == null || animationSequence == null || animationSequence.Animations == null) return;
            //if (LastRequest + 1 > Mission.Current.CurrentTime) return;
            //LastRequest = Mission.Current.CurrentTime;
            try
            {
                foreach (Animation animation in animationSequence.Animations)
                {
                    Log($"Playing {animation.Name}", LogLevel.Debug);
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new RequestAnimationFormation(target, animation.Index, animation.Speed));
                    GameNetwork.EndModuleEventAsClient();
                    await Task.Delay(TimeSpan.FromSeconds(animation.MaxDuration));
                }
            }
            catch (Exception ex)
            {
                Log($"Failure in sequence {animationSequence.Name} on formation {target.PrimaryClass}", LogLevel.Error);
                Log(ex.Message, LogLevel.Error);
            }
        }
    }
}