using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.NetworkMessages.FromServer;
using Alliance.Common.Extensions;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Core.Configuration.Handlers
{
    public class ConfigHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncConfigField>(HandleSyncConfigField);
            reg.Register<SyncConfigAll>(HandleSyncConfigAll);
        }

        public void HandleSyncConfigField(SyncConfigField message)
        {
            try
            {
                ConfigManager.Instance.UpdateConfigField(message.FieldIndex, message.FieldValue);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to update configuration : {message.FieldIndex} | {message.FieldValue}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        public void HandleSyncConfigAll(SyncConfigAll message)
        {
            try
            {
                Config.Instance = message.Config;
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to update configuration (all)", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
