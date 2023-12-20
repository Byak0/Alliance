using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class RemoveEntity : GameNetworkMessage
    {
        public Vec3 Position { get; private set; }

        public RemoveEntity()
        {
        }

        public RemoveEntity(Vec3 position)
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
            return $"Remove entity at position {Position}";
        }
    }
}
