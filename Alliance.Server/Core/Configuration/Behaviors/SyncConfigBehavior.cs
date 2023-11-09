using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Configuration.Behaviors
{
    /// <summary>
    /// MissionBehavior used to synchronize configuration between server and clients
    /// </summary>
    public class SyncConfigBehavior : MissionNetwork, IMissionBehavior
    {
        public SyncConfigBehavior() : base()
        {
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            if (Config.Instance.SyncConfig) SyncConfig(networkPeer);
        }

        public void SyncConfig(NetworkCommunicator networkPeer)
        {
            try
            {
                ConfigManager.Instance.SendConfigToPeer(networkPeer);
                Log($"Alliance - Successfully sent config to {networkPeer.UserName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Error sending config to {networkPeer.UserName}", LogLevel.Error);
                Log(ex.Message, LogLevel.Error);
            }
        }
    }
}