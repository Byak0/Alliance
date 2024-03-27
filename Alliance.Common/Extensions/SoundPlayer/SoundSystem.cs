using Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromServer;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
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
        /// Play sound at position with specified duration.
        /// </summary>
        /// <param name="synchronize">Set to true to synchronize with all clients</param>
        public void PlaySound(int soundIndex, Vec3 position, int soundDuration = -1, bool synchronize = false)
        {
            SoundEvent eventRef = SoundEvent.CreateEvent(soundIndex, Mission.Current?.Scene);
            if (position != Vec3.Invalid) eventRef.SetPosition(position);
            eventRef.Play();
            if (soundDuration != -1) DelayedStop(eventRef, soundDuration * 1000);

            if (synchronize)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncSoundLocalized(soundIndex, soundDuration, position));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        public void PlaySound(string soundName, int soundDuration = -1, bool synchronize = false)
        {
            Vec3 position = Agent.Main?.Position ?? Vec3.Invalid;
            Log($"Alliance - Playing sound {soundName} at {position} for {soundDuration}s", LogLevel.Debug);
            PlaySound(SoundEvent.GetEventIdFromString(soundName), position, soundDuration, synchronize);
        }

        public void PlaySoundLocalized(string soundName, Vec3 position, int soundDuration = -1, bool synchronize = false)
        {
            Log($"Alliance - Playing localized sound {soundName} at {position} for {soundDuration}s", LogLevel.Debug);
            PlaySound(SoundEvent.GetEventIdFromString(soundName), position, soundDuration, synchronize);
        }

        // Delayed stop to prevent ambient sounds from looping
        private async void DelayedStop(SoundEvent eventRef, int soundDuration)
        {
            await Task.Delay(soundDuration);
            eventRef.Stop();
        }
    }
}
