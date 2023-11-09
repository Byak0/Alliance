using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ShrinkingZone.Behaviors;
using Alliance.Common.Extensions.ShrinkingZone.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.ShrinkingZone.Handlers
{
    public class ShrinkingZoneHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncShrinkingZone>(HandleSyncShrinkingZone);
        }

        public void HandleSyncShrinkingZone(SyncShrinkingZone message)
        {
            ShrinkingZoneBehavior brZoneBehavior = Mission.Current.GetMissionBehavior<ShrinkingZoneBehavior>();
            brZoneBehavior.InitZone(message.Origin, message.Radius, message.LifeTime, message.Visible);
        }
    }
}
