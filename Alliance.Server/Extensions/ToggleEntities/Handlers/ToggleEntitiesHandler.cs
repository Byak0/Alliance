using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromClient;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.ToggleEntities.Handlers
{
    public class ToggleEntitiesHandler : IHandlerRegister
    {
        public ToggleEntitiesHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestToggleEntities>(HandleToggleEntities);
        }

        public bool HandleToggleEntities(NetworkCommunicator peer, RequestToggleEntities message)
        {
            if (!peer.IsAdmin())
            {
                Log($"ATTENTION : {peer.UserName} is requesting to {(message.Show ? "show" : "hide")} entities with tag {message.EntitiesTag} despite not being admin !", LogLevel.Error);
                return false;
            }
            Log($"Alliance - {peer.UserName} is requesting to {(message.Show ? "show" : "hide")} entities with tag {message.EntitiesTag}.", LogLevel.Information);

            foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag(message.EntitiesTag))
            {
                entity.SetVisibilityExcludeParents(message.Show);
            }

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new SyncToggleEntities(message.EntitiesTag, message.Show));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

            return true;
        }
    }
}
