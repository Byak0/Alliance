using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.SoundPlayer;
using Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromClient;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.SoundPlayer.Handlers
{
    public class SoundHandler : IHandlerRegister
    {
        public SoundHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SoundRequest>(HandleSoundRequest);
        }

        public bool HandleSoundRequest(NetworkCommunicator peer, SoundRequest message)
        {
            try
            {
                if (!peer.IsAdmin())
                {
                    Log($"ATTENTION : {peer.UserName} sent a SoundRequest despite not being admin !", LogLevel.Error);
                    return false;
                }
                Log($"Alliance - {peer.UserName} is requesting to play sound {message.SoundIndex} for {message.SoundDuration} seconds.", LogLevel.Information);
                SoundSystem.Instance.PlaySound(message.SoundIndex, Vec3.Invalid, message.SoundDuration, true);
                return true;
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to play sound {message.SoundIndex} for {message.SoundDuration} seconds as requested by {peer.UserName}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
            return false;
        }
    }
}
