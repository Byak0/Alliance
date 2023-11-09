using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class TeleportRequest : GameNetworkMessage
    {
        public Vec3 Position { get; set; }

        public TeleportRequest() { }

        public TeleportRequest(Vec3 position)
        {
            Position = position;
        }

        protected override void OnWrite()
        {
            WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Position = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Administration;
        }

        protected override string OnGetLogFormat()
        {
            return "TeleportRequest";
        }
    }
}
