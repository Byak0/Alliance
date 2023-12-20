using Alliance.Common.Extensions;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Client.Extensions.UsableEntity.Handlers
{
    public class UsableEntityHandler : IHandlerRegister
    {
        UsableEntityBehavior _gameMode;

        UsableEntityBehavior GameMode
        {
            get
            {
                return _gameMode ??= Mission.Current.GetMissionBehavior<UsableEntityBehavior>();
            }
        }

        public void Register(NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RemoveEntity>(RemoveEntity);
        }

        public void RemoveEntity(RemoveEntity message)
        {
            try
            {
                GameMode.RemoveEntity(GameMode.FindClosestUsableEntity(message.Position, 0.1f));
            }
            catch (Exception ex)
            {
                Log($"Alliance - Failed to remove entity at {message.Position}", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
