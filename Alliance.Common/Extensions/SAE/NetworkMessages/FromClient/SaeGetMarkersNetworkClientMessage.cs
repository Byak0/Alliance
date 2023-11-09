using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeGetMarkersNetworkClientMessage : GameNetworkMessage
    {
        public int Side { get; private set; }
        readonly CompressionInfo.Integer CompressionInfo;

        public SaeGetMarkersNetworkClientMessage()
        {
            CompressionInfo = new CompressionInfo.Integer(0, 10, true);
        }
        public SaeGetMarkersNetworkClientMessage(int side)
        {
            Side = side;
            CompressionInfo = new CompressionInfo.Integer(0, 10, true);
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(Side, CompressionInfo);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;

            Side = ReadIntFromPacket(CompressionInfo, ref bufferReadValid);

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Formations;
        }
        protected override string OnGetLogFormat()
        {
            return "Request to server to give to peer the list of marker of peer's team";
        }

    }
}
