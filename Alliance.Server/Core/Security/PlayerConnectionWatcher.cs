using Alliance.Common.Core.Security.Extension;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;

namespace Alliance.Server.Core.Security
{
    /// <summary>
    /// Watch who connect to the server and take... appropriate measures.
    /// </summary>
    public class PlayerConnectionWatcher : GameHandler
    {
        protected override void OnPlayerConnect(VirtualPlayer peer)
        {
            if (peer.IsBanned())
            {
                DedicatedCustomServerSubModule.Instance.DedicatedCustomGameServer.KickPlayer(peer.Id, false);
            }
        }

        public override void OnAfterSave()
        {
        }

        public override void OnBeforeSave()
        {
        }

        public PlayerConnectionWatcher()
        {
        }
    }
}
