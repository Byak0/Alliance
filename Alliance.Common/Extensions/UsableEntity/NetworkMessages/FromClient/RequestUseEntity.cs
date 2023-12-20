using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestUseEntity : GameNetworkMessage
    {
        public Vec3 Position { get; private set; }

        public RequestUseEntity() { }

        public RequestUseEntity(Vec3 position)
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
            return MultiplayerMessageFilter.MissionObjects;
        }
        protected override string OnGetLogFormat()
        {
            return $"Player request to use entity at {Position}";
        }
    }
}
