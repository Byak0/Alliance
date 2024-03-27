using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SoundPlayer;
using Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromServer;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.SoundPlayer.Handlers
{
    public class SoundClientHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncSoundLocalized>(HandleSyncSoundLocalized);
            reg.Register<SyncSound>(HandleSyncSound);
        }

        public void HandleSyncSoundLocalized(SyncSoundLocalized message)
        {
            try
            {
                Log($"Alliance - Playing sound {message.SoundIndex} at {message.Position} for {message.SoundDuration}", LogLevel.Debug);
                SoundSystem.Instance.PlaySound(message.SoundIndex, message.Position, message.SoundDuration);
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
                SoundSystem.Instance.PlaySound(message.SoundIndex, Vec3.Invalid, message.SoundDuration);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play sound {message.SoundIndex}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
