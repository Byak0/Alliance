using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SoundPlayer;
using Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromServer;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.SoundPlayer.Handlers
{
    public class SoundClientHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncSound>(HandleSyncSound);
        }

        public void HandleSyncSound(SyncSound message)
        {
            try
            {
                SoundSystem.Instance.PlaySound(message.SoundIndex, message.SoundDuration);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play sound {message.SoundIndex}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
