using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeDeleteAllMarkersForAllNetworkClientMessage : GameNetworkMessage
    {
        public SaeDeleteAllMarkersForAllNetworkClientMessage()
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
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Request to delete all markers";
        }
    }
}
