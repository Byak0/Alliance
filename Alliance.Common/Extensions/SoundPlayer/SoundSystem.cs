using Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromServer;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.SoundPlayer
{
    /// <summary>
    /// Handle playing sounds and their synchronization.    
    /// </summary>
    public class SoundSystem
    {
        private static SoundSystem _instance;

        public static SoundSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SoundSystem();
                }
                return _instance;
            }
        }

        public SoundSystem()
        {
        }

        /// <summary>
        /// Play sound with specified duration.
        /// </summary>
        /// <param name="synchronize">Set to true to synchronize with all clients</param>
        public void PlaySound(int soundIndex, int soundDuration = -1, bool synchronize = false)
        {
            SoundEvent eventRef = SoundEvent.CreateEvent(soundIndex, Mission.Current?.Scene);
            Log("Alliance - Playing sound " + soundIndex, LogLevel.Debug);
            if (Agent.Main?.Position != null) eventRef.SetPosition(Agent.Main.Position);
            eventRef.Play();
            if (soundDuration != -1) DelayedStop(eventRef, soundDuration * 1000);

            if (synchronize)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncSound(soundIndex, soundDuration));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        public void PlaySound(string soundName, int soundDuration = -1, bool synchronize = false)
        {
            PlaySound(SoundEvent.GetEventIdFromString(soundName), soundDuration, synchronize);
        }

        // Delayed stop to prevent ambient sounds from looping
        private async void DelayedStop(SoundEvent eventRef, int soundDuration)
        {
            await Task.Delay(soundDuration);
            eventRef.Stop();
        }
    }
}
