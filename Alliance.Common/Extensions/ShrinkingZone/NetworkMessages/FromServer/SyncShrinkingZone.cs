using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.ShrinkingZone.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncShrinkingZone : GameNetworkMessage
    {
        public Vec3 Origin { get; private set; }
        public float Radius { get; private set; }
        public int LifeTime { get; private set; }
        public bool Visible { get; private set; }

        public SyncShrinkingZone()
        {
        }

        public SyncShrinkingZone(Vec3 zoneOrigin, float zoneRadius, int zoneLifeTime, bool visible)
        {
            Origin = zoneOrigin;
            Radius = zoneRadius;
            LifeTime = zoneLifeTime;
            Visible = visible;
        }

        protected override void OnWrite()
        {
            WriteVec3ToPacket(Origin, CompressionBasic.PositionCompressionInfo);
            WriteFloatToPacket(Radius, CompressionBasic.PositionCompressionInfo);
            WriteIntToPacket(LifeTime, CompressionBasic.RoundTimeLimitCompressionInfo);
            WriteBoolToPacket(Visible);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Origin = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
            Radius = ReadFloatFromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
            LifeTime = ReadIntFromPacket(CompressionBasic.RoundTimeLimitCompressionInfo, ref bufferReadValid);
            Visible = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Synchronize the shrinking zone.";
        }
    }
}
