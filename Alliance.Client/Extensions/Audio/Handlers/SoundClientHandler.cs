using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.Audio.NetworkMessages.FromServer;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.Audio.Handlers
{
    public class SoundClientHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncSoundLocalized>(HandleSyncSoundLocalized);
            reg.Register<SyncSound>(HandleSyncSound);
            reg.Register<SyncAudioLocalized>(HandleSyncAudioLocalized);
            reg.Register<SyncAudio>(HandleSyncAudio);
        }

        private void HandleSyncAudio(SyncAudio message)
        {
            try
            {
                Log($"Alliance - Playing audio {message.SoundIndex} with volume {message.Volume}", LogLevel.Debug);
                AudioPlayer.Instance.Play(message.SoundIndex, message.Volume, message.Stackable);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play audio {message.SoundIndex}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        private void HandleSyncAudioLocalized(SyncAudioLocalized message)
        {
            try
            {
                Log($"Alliance - Playing audio {message.SoundIndex} at {message.SoundOrigin} with volume {message.Volume}", LogLevel.Debug);
                AudioPlayer.Instance.Play(message.SoundIndex, message.Volume, message.Stackable, message.MaxHearingDistance, message.SoundOrigin);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play audio {message.SoundIndex}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        public void HandleSyncSoundLocalized(SyncSoundLocalized message)
        {
            try
            {
                Log($"Alliance - Playing sound {message.SoundIndex} at {message.Position} for {message.SoundDuration}", LogLevel.Debug);
                NativeAudioPlayer.Instance.PlaySound(message.SoundIndex, message.Position, message.SoundDuration);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play sound {message.SoundIndex}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        public void HandleSyncSound(SyncSound message)
        {
            try
            {
                Log($"Alliance - Playing sound {message.SoundIndex} for {message.SoundDuration}", LogLevel.Debug);
                NativeAudioPlayer.Instance.PlaySound(message.SoundIndex, Vec3.Invalid, message.SoundDuration);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play sound {message.SoundIndex}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
