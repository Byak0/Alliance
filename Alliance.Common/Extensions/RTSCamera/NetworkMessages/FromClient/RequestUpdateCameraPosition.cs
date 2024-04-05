using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.RTSCamera.NetworkMessages.FromClient
{
    /// <summary>
    /// Request to update camera position.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestUpdateCameraPosition : GameNetworkMessage
    {
        public Vec3 Position { get; private set; }

        public RequestUpdateCameraPosition() { }

        public RequestUpdateCameraPosition(Vec3 position)
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
            return MultiplayerMessageFilter.Peers;
        }

        protected override string OnGetLogFormat()
        {
            return string.Empty;
        }
    }
}
