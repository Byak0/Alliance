using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.NetworkMessages.FromServer;
using Alliance.Common.Extensions;
using System;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Client.Core.Security.Handlers
{
    public class RoleHandler : IHandlerRegister
    {
        public void Register(NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<UpdateRole>(HandleUpdateRole);
        }

        public void HandleUpdateRole(UpdateRole message)
        {
            try
            {
                RoleManager.Instance.UpdatePlayersRoles(message.Role, message.PlayerId, message.Remove);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to {(message.Remove ? "remove" : "add")} player as {RoleManager.Instance.RolesFields[message.Role].Name}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
