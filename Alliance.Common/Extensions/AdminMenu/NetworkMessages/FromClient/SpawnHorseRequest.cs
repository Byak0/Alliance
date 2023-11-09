using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SpawnHorseRequest : GameNetworkMessage
    {
        public SpawnHorseRequest() { }

        protected override void OnWrite()
        {
        }

        protected override bool OnRead()
        {
            return true;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return "SpawnHorseRequest";
        }
    }
}
