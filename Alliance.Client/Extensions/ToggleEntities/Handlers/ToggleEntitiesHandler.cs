using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.ToggleEntities.Handlers
{
    public class ToggleEntitiesHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncToggleEntities>(HandleToggleEntitiesVisibility);
        }

        public void HandleToggleEntitiesVisibility(SyncToggleEntities message)
        {
            if (Mission.Current?.Scene == null) return;

            foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag(message.EntitiesTag))
            {
                entity.SetVisibilityExcludeParents(message.Show);
            }
        }
    }
}