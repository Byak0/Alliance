using Alliance.Common.Core.Security;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Security.Behaviors
{
    /// <summary>
    /// MissionBehavior used to synchronize access and roles between server and clients
    /// </summary>
    public class SyncRolesBehavior : MissionNetwork, IMissionBehavior
    {
        public SyncRolesBehavior() : base()
        {
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            SyncPlayersRoles(networkPeer);
        }

        public void SyncPlayersRoles(NetworkCommunicator networkPeer)
        {
            try
            {
                RoleManager.Instance.SendPlayersRolesToPeer(networkPeer);
                Log($"Alliance - Successfully sent roles to {networkPeer.UserName}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Error sending roles to {networkPeer.UserName}", LogLevel.Error);
                Log(ex.Message, LogLevel.Error);
            }
        }
    }
}