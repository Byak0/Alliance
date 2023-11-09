using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestNotification : GameNetworkMessage
    {
        public string Text { get; set; }
        public int NotificationType { get; set; }

        public RequestNotification() { }

        public RequestNotification(string text, int notificationType)
        {
            Text = text;
            NotificationType = notificationType;
        }

        protected override void OnWrite()
        {
            WriteStringToPacket(Text);
            WriteIntToPacket(NotificationType, new CompressionInfo.Integer(0, 2, true));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Text = ReadStringFromPacket(ref bufferReadValid);
            NotificationType = ReadIntFromPacket(new CompressionInfo.Integer(0, 2, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return "RequestNotification " + Text;
        }
    }
}
