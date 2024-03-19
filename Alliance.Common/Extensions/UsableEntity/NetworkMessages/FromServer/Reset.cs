using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class Reset : GameNetworkMessage
    {
        // This empty constructor is required so the engine recognize this class as a valid NetworkMessage
        public Reset() { }

        protected override void OnWrite()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjects;
        }
        protected override string OnGetLogFormat()
        {
            return $"Ask client to make object visible again";
        }
    }
}
