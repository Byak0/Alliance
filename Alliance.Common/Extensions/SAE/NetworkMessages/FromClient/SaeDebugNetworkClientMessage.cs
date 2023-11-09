using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeDebugNetworkClientMessage : GameNetworkMessage
    {

        public SaeDebugNetworkClientMessage()
        {
        }

        protected override void OnWrite()
        {
        }

        protected override bool OnRead()
        {
            return true;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Formations;
        }
        protected override string OnGetLogFormat()
        {
            return "DEBUG ASKED!";
        }

    }
}
